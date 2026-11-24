using System.Data.Common;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Application.Coletas.ExecutarColeta;

public sealed class ColetaExecutor(
    IFonteRepository fonteRepository,
    IExecucaoColetaRepository execucaoRepository,
    IDocumentoRepository documentoRepository,
    ICollectorResolver collectorResolver,
    IUnitOfWork unitOfWork,
    IAuditoriaService auditoriaService,
    IFonteColetaLock fonteColetaLock,
    TimeProvider timeProvider,
    ColetaMetrics metrics,
    ILogger<ColetaExecutor> logger) : IColetaExecutor
{
    public async Task<ExecucaoColetaResponse> ExecutarManualAsync(
        Guid fonteId,
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await ExecutarAsync(fonteId, usuarioId, false, cancellationToken)
            ?? throw new InvalidOperationException("A coleta manual não foi iniciada.");

    public Task<ExecucaoColetaResponse?> ExecutarAutomaticaSeVencidaAsync(
        Guid fonteId,
        CancellationToken cancellationToken,
        CancellationToken encerramentoToken = default) =>
        ExecutarAsync(fonteId, null, true, cancellationToken, encerramentoToken);

    public async Task RecuperarInterrompidaAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        await using var lease = await fonteColetaLock.TentarAdquirirAsync(
            fonteId, cancellationToken);
        if (lease is null)
            return;

        if (await fonteRepository.ObterPorIdAsync(fonteId, cancellationToken) is null ||
            !await lease.TentarReivindicarAsync(cancellationToken))
            return;

        await RecuperarSobLockAsync(fonteId, cancellationToken);
    }

    private async Task<ExecucaoColetaResponse?> ExecutarAsync(
        Guid fonteId,
        Guid? usuarioId,
        bool automatica,
        CancellationToken cancellationToken,
        CancellationToken encerramentoToken = default)
    {
        await using var lease = await fonteColetaLock.TentarAdquirirAsync(
            fonteId, cancellationToken);
        if (lease is null)
        {
            logger.LogDebug(
                "Coleta não iniciada: outra execução detém a Fonte {FonteId}.", fonteId);

            if (automatica)
                return null;
            throw new BusinessException("Já existe uma coleta em andamento para esta fonte.");
        }

        var fonte = await fonteRepository.ObterPorIdAsync(fonteId, cancellationToken);
        if (fonte is null)
        {
            if (automatica)
                return null;
            throw new NotFoundException("Fonte não encontrada.");
        }

        using var fonteScope = logger.BeginScope(
            "FonteId:{FonteId} FonteNome:{FonteNome} FonteTipo:{FonteTipo}",
            fonte.Id, fonte.Nome, fonte.Tipo);

        if (!await lease.TentarReivindicarAsync(cancellationToken))
        {
            if (automatica)
                return null;
            throw new BusinessException("Já existe uma coleta em andamento para esta fonte.");
        }

        await RecuperarSobLockAsync(fonteId, cancellationToken);

        if (fonte.Status != StatusFonte.Ativa)
        {
            if (automatica)
                return null;
            throw new BusinessException("A fonte precisa estar ativa para realizar a coleta.");
        }

        // O worker já filtra as pausadas; esta checagem cobre a pausa feita entre a listagem e a coleta.
        if (automatica && fonte.AgendamentoPausado)
            return null;

        if (automatica)
        {
            var ultima = await execucaoRepository.ObterUltimaPorFonteAsync(
                fonteId, cancellationToken);
            var referencia = ultima?.Fim ?? ultima?.Inicio ?? fonte.CriadoEm;
            if (timeProvider.GetUtcNow() < referencia.AddMinutes(fonte.PeriodicidadeMinutos))
                return null;
        }

        var execucao = automatica
            ? ExecucaoColeta.IniciarAutomatica(fonteId)
            : ExecucaoColeta.IniciarManual(fonteId, usuarioId!.Value);
        await execucaoRepository.AdicionarAsync(execucao, cancellationToken);
        var inicioMedicao = Stopwatch.GetTimestamp();

        using var coletaScope = logger.BeginScope(
            "ColetaId:{ColetaId} UsuarioId:{UsuarioId} Automatica:{Automatica}",
            execucao.Id, usuarioId, automatica);

        logger.LogInformation(
            "Coleta {Modo} iniciada para {Url}",
            automatica ? "automática" : "manual", fonte.Url);

        var transacaoIniciada = false;
        try
        {
            if (automatica)
            {
                transacaoIniciada = true;
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                transacaoIniciada = false;
            }

            var collector = collectorResolver.Resolver(fonte.Tipo);
            await lease.RenovarAsync(cancellationToken);
            var coletados = await ColetarMonitoradoAsync(
                lease, collector, fonte.Url, fonte.SeletorConteudo, cancellationToken);

            logger.LogInformation(
                "Coletor {Coletor} retornou {Quantidade} documento(s)",
                collector.GetType().Name, coletados.Count);

            var documentos = coletados
                .Select(documento =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new Documento(
                        execucao.Id, documento.Titulo, documento.UrlOriginal,
                        documento.Tipo, documento.Origem, fonte.Contexto,
                        documento.DataPublicacao, documento.ConteudoTextual,
                        documento.Metadados, documento.Hash);
                })
                .DistinctBy(documento => (documento.UrlOriginal, documento.Hash))
                .ToList();

            await lease.VerificarSessaoAsync(cancellationToken);
            transacaoIniciada = true;
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            if (!automatica)
                await unitOfWork.CommitAsync(cancellationToken);

            var quantidadeInserida = await documentoRepository.InserirNovosAsync(
                documentos, cancellationToken);
            await fonteRepository.ZerarFalhasConsecutivasAsync(fonte.Id, cancellationToken);
            execucao.Concluir(quantidadeInserida);
            if (!automatica)
            {
                await auditoriaService.RegistrarAsync(
                    TipoEventoAuditoria.Coleta,
                    $"Coleta manual concluída ({quantidadeInserida} documentos)",
                    fonte.Nome,
                    fonte.Id,
                    cancellationToken);
            }
            await unitOfWork.CommitAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            transacaoIniciada = false;

            logger.LogInformation(
                "Coleta concluída: {Inseridos} inserido(s), {Ignorados} já existente(s)",
                quantidadeInserida, documentos.Count - quantidadeInserida);

            metrics.RegistrarSucesso(
                fonte.Tipo, Stopwatch.GetElapsedTime(inicioMedicao), quantidadeInserida);

            return ExecucaoColetaResponse.FromEntity(execucao);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Coleta cancelada antes de terminar.");

            // Encerramento da aplicação não é falha de coleta; timeout da automática é.
            if (automatica && !encerramentoToken.IsCancellationRequested)
                metrics.RegistrarFalha(fonte.Tipo, Stopwatch.GetElapsedTime(inicioMedicao));

            if (transacaoIniciada)
                await RollbackSemMascararAsync(execucao.Id);

            if (automatica)
                await RegistrarFalhaAutomaticaSemMascararAsync(
                    execucao.Id, fonte,
                    encerramentoToken.IsCancellationRequested
                        ? "Coleta cancelada pelo encerramento da aplicação."
                        : cancellationToken.IsCancellationRequested &&
                          encerramentoToken.CanBeCanceled
                            ? "Tempo limite da coleta excedido."
                            : "Coleta cancelada.",
                    contarFalha: !encerramentoToken.IsCancellationRequested);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Coleta falhou para {Url}", fonte.Url);

            metrics.RegistrarFalha(fonte.Tipo, Stopwatch.GetElapsedTime(inicioMedicao));

            var readicionarManual = transacaoIniciada;
            if (transacaoIniciada)
                await RollbackSemMascararAsync(execucao.Id);

            if (automatica)
            {
                await RegistrarFalhaAutomaticaSemMascararAsync(
                    execucao.Id, fonte, exception.Message, contarFalha: true);
            }
            else
            {
                execucao.RegistrarFalha(exception.Message);
                try
                {
                    if (readicionarManual)
                        await execucaoRepository.AdicionarAsync(execucao, CancellationToken.None);
                    using var auditCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await auditoriaService.RegistrarAsync(
                        TipoEventoAuditoria.Coleta,
                        "Coleta manual com falha",
                        fonte.Nome,
                        fonte.Id,
                        auditCts.Token);
                    await unitOfWork.BeginTransactionAsync(auditCts.Token);
                    await RegistrarFalhaNaFonteAsync(fonte, exception.Message, auditCts.Token);
                    await unitOfWork.CommitAsync(auditCts.Token);
                    await unitOfWork.CommitTransactionAsync(auditCts.Token);
                }
                catch (Exception auditException)
                {
                    await RollbackSemMascararAsync(execucao.Id);
                    logger.LogError(auditException,
                        "Falha ao registrar a execução {ExecucaoId} como falha após o erro original.",
                        execucao.Id);
                    logger.LogError(exception,
                        "Erro original da execução de coleta {ExecucaoId}.", execucao.Id);
                    ExceptionDispatchInfo.Capture(exception).Throw();
                    throw;
                }
            }

            if (EhErroDePersistencia(exception))
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw new InvalidOperationException("Fluxo inalcançável.");
            }

            throw new BusinessException("A coleta não pôde ser concluída.");
        }
    }

    private async Task RecuperarSobLockAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        var abertas = await execucaoRepository.ListarEmAndamentoPorFonteAsync(
            fonteId, cancellationToken);
        if (abertas.Count == 0)
            return;

        foreach (var aberta in abertas)
            aberta.RegistrarFalha("Coleta interrompida antes da conclusão.");
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task RegistrarFalhaAutomaticaSemMascararAsync(
        Guid execucaoId,
        Fonte fonte,
        string mensagem,
        bool contarFalha)
    {
        var fonteId = fonte.Id;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await fonteColetaLock.VerificarSessaoAsync(cts.Token);
            var persistida = await execucaoRepository.ObterPorIdAsync(execucaoId, cts.Token);
            if (persistida?.Status != StatusExecucaoColeta.EmAndamento)
                return;

            var abertas = await execucaoRepository.ListarEmAndamentoPorFonteAsync(
                fonteId, cts.Token);
            var atual = abertas.FirstOrDefault(item => item.Id == execucaoId);
            if (atual is null)
                return;

            atual.RegistrarFalha(mensagem);
            await unitOfWork.BeginTransactionAsync(cts.Token);
            try
            {
                if (contarFalha)
                    await RegistrarFalhaNaFonteAsync(fonte, mensagem, cts.Token);
                await unitOfWork.CommitAsync(cts.Token);
                await unitOfWork.CommitTransactionAsync(cts.Token);
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cts.Token);
                throw;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Falha ao encerrar a execução automática {ExecucaoId}; o worker a recuperará.",
                execucaoId);
        }
    }

    /// <summary>
    /// Soma a falha ao contador da fonte e, ao atingir o limite, registra um alerta
    /// no histórico. Deve rodar dentro da transação que encerra a execução com falha.
    /// </summary>
    private async Task RegistrarFalhaNaFonteAsync(
        Fonte fonte,
        string motivo,
        CancellationToken cancellationToken)
    {
        var falhas = await fonteRepository.IncrementarFalhasConsecutivasAsync(
            fonte.Id, cancellationToken);
        if (!Fonte.DeveAlertarFalhas(falhas))
            return;

        logger.LogWarning(
            "Fonte {FonteId} atingiu {Falhas} falhas consecutivas de coleta. Último motivo: {Motivo}",
            fonte.Id, falhas, motivo);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Alerta,
            $"{falhas} falhas consecutivas de coleta: {motivo}",
            fonte.Nome,
            fonte.Id,
            null,
            AuditoriaService.ResponsavelSistema,
            cancellationToken);
    }

    private async Task RollbackSemMascararAsync(Guid execucaoId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await unitOfWork.RollbackTransactionAsync(cts.Token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Falha ao reverter a transação da execução de coleta {ExecucaoId}.", execucaoId);
        }
    }

    private static async Task<IReadOnlyList<CollectedDocument>> ColetarMonitoradoAsync(
        IFonteColetaLease lease,
        IContentCollector collector,
        string url,
        string? seletorConteudo,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var monitorStop = new CancellationTokenSource();
        Exception? perda = null;
        var monitor = MonitorarAsync();
        IReadOnlyList<CollectedDocument>? coletados = null;
        ExceptionDispatchInfo? erroColetor = null;

        try
        {
            coletados = await collector.ColetarAsync(url, seletorConteudo, linked.Token);
        }
        catch (Exception exception)
        {
            erroColetor = ExceptionDispatchInfo.Capture(exception);
        }
        finally
        {
            monitorStop.Cancel();
            await monitor;
        }

        if (perda is not null)
            throw new ColetaPossePerdidaException("A posse foi perdida durante a coleta.", perda);
        erroColetor?.Throw();
        cancellationToken.ThrowIfCancellationRequested();
        return coletados!;

        async Task MonitorarAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), monitorStop.Token);
                    await lease.RenovarAsync(monitorStop.Token);
                }
            }
            catch (OperationCanceledException) when (monitorStop.IsCancellationRequested)
            {
                // Encerramento supervisionado do monitor.
            }
            catch (Exception exception)
            {
                perda = exception;
                linked.Cancel();
            }
        }
    }

    private static bool EhErroDePersistencia(Exception exception)
    {
        for (var atual = exception; atual is not null; atual = atual.InnerException!)
        {
            if (atual is DbException)
                return true;
        }
        return false;
    }
}