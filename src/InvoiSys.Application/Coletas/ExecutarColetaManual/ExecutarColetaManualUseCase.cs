using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Coletas.ExecutarColetaManual;

public sealed class ExecutarColetaManualUseCase(
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IExecucaoColetaRepository execucaoRepository,
    IDocumentoRepository documentoRepository,
    ICollectorResolver collectorResolver,
    IUnitOfWork unitOfWork) : IExecutarColetaManualUseCase
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

        var execucao = ExecucaoColeta.IniciarManual(fonte.Id, usuarioId);
        await execucaoRepository.AdicionarAsync(execucao, cancellationToken);

        try
        {
            var collector = collectorResolver.Resolver(fonte.Tipo);
            var coletados = await collector.ColetarAsync(fonte.Url, cancellationToken);

            var documentos = coletados
                .Select(documento => new Documento(
                    execucao.Id,
                    documento.Titulo,
                    documento.UrlOriginal,
                    documento.Tipo,
                    documento.Origem,
                    documento.DataPublicacao,
                    documento.ConteudoTextual,
                    documento.Metadados,
                    documento.Hash))
                .ToList();

            await documentoRepository.AdicionarVariosAsync(documentos, cancellationToken);

            execucao.Concluir(documentos.Count);
            await unitOfWork.CommitAsync(cancellationToken);

            return ExecucaoColetaResponse.FromEntity(execucao);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            execucao.RegistrarFalha(exception.Message);
            await unitOfWork.CommitAsync(cancellationToken);
            throw new BusinessException("A coleta não pôde ser concluída.");
        }
    }
}
