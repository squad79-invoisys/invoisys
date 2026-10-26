using System.Data.Common;
using System.Runtime.ExceptionServices;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Application.Coletas.ExecutarColetaManual;

public sealed class ExecutarColetaManualUseCase(
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IExecucaoColetaRepository execucaoRepository,
    IDocumentoRepository documentoRepository,
    ICollectorResolver collectorResolver,
    IUnitOfWork unitOfWork,
    ILogger<ExecutarColetaManualUseCase> logger) : IExecutarColetaManualUseCase
{
    public async Task<ExecucaoColetaResponse> ExecutarAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        var fonte = await fonteRepository.ObterPorIdAsync(fonteId, cancellationToken)
            ?? throw new NotFoundException("Fonte não encontrada.");

        if (fonte.Status != StatusFonte.Ativa)
            throw new BusinessException("A fonte precisa estar ativa para realizar a coleta.");

        using var fonteScope = logger.BeginScope(
            "FonteId:{FonteId} FonteNome:{FonteNome} FonteTipo:{FonteTipo}",
            fonte.Id, fonte.Nome, fonte.Tipo);

        var execucao = ExecucaoColeta.IniciarManual(fonte.Id, usuarioId);

        using var coletaScope = logger.BeginScope(
            "ColetaId:{ColetaId} UsuarioId:{UsuarioId}",
            execucao.Id, usuarioId);

        logger.LogInformation("Coleta manual iniciada para {Url}", fonte.Url);

        await execucaoRepository.AdicionarAsync(execucao, cancellationToken);
        var transacaoIniciada = false;

        try
        {
            var collector = collectorResolver.Resolver(fonte.Tipo);
            var coletados = await collector.ColetarAsync(
                fonte.Url,
                fonte.SeletorConteudo,
                cancellationToken);

            logger.LogInformation(
                "Coletor {Coletor} retornou {Quantidade} documento(s)",
                collector.GetType().Name, coletados.Count);

            var documentos = coletados
                .Select(documento => new Documento(
                    execucao.Id,
                    documento.Titulo,
                    documento.UrlOriginal,
                    documento.Tipo,
                    documento.Origem,
                    fonte.Contexto,
                    documento.DataPublicacao,
                    documento.ConteudoTextual,
                    documento.Metadados,
                    documento.Hash))
                .DistinctBy(documento => (documento.UrlOriginal, documento.Hash))
                .ToList();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transacaoIniciada = true;

            // Persiste a execução dentro da mesma transação para satisfazer a FK,
            // sem torná-la visível como EmAndamento fora da transação.
            await unitOfWork.CommitAsync(cancellationToken);

            var quantidadeInserida = await documentoRepository.InserirNovosAsync(
                documentos,
                cancellationToken);

            execucao.Concluir(quantidadeInserida);
            await unitOfWork.CommitAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            transacaoIniciada = false;

            logger.LogInformation(
                "Coleta concluída: {Inseridos} inserido(s), {Ignorados} já existente(s)",
                quantidadeInserida, documentos.Count - quantidadeInserida);

            return ExecucaoColetaResponse.FromEntity(execucao);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Coleta cancelada antes de terminar.");

            if (transacaoIniciada)
                await RollbackSemMascararAsync();

            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Coleta falhou para {Url}", fonte.Url);

            var execucaoPrecisaSerReadicionada = transacaoIniciada;

            if (transacaoIniciada)
            {
                await RollbackSemMascararAsync();
            }

            execucao.RegistrarFalha(exception.Message);

            try
            {
                if (execucaoPrecisaSerReadicionada)
                {
                    // Após rollback o tracker foi limpo e a inserção anterior da
                    // execução também foi revertida; ela precisa ser adicionada novamente.
                    await execucaoRepository.AdicionarAsync(execucao, CancellationToken.None);
                }

                await unitOfWork.CommitAsync(CancellationToken.None);
            }
            catch (Exception auditException)
            {
                logger.LogError(
                    auditException,
                    "Falha ao registrar a execução {ExecucaoId} como falha após o erro original.",
                    execucao.Id);
                logger.LogError(
                    exception,
                    "Erro original da execução de coleta {ExecucaoId}.",
                    execucao.Id);

                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }

            if (EhErroDePersistencia(exception))
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw new InvalidOperationException("Fluxo inalcançável.");
            }

            throw new BusinessException("A coleta não pôde ser concluída.");
        }

        async Task RollbackSemMascararAsync()
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogError(
                    rollbackException,
                    "Falha ao reverter a transação da execução de coleta {ExecucaoId}.",
                    execucao.Id);
            }
            finally
            {
                transacaoIniciada = false;
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