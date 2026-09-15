namespace InvoiSys.Application.Coletas.ExecutarColetaManual;

public interface IExecutarColetaManualUseCase
{
    Task<ExecucaoColetaResponse> ExecutarAsync(
        Guid fonteId,
        CancellationToken cancellationToken);
}
