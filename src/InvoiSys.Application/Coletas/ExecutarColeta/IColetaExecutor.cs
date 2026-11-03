namespace InvoiSys.Application.Coletas.ExecutarColeta;

public interface IColetaExecutor
{
    Task<ExecucaoColetaResponse> ExecutarManualAsync(
        Guid fonteId,
        Guid usuarioId,
        CancellationToken cancellationToken);

    Task<ExecucaoColetaResponse?> ExecutarAutomaticaSeVencidaAsync(
        Guid fonteId,
        CancellationToken cancellationToken,
        CancellationToken encerramentoToken = default);

    Task RecuperarInterrompidaAsync(
        Guid fonteId,
        CancellationToken cancellationToken);
}
