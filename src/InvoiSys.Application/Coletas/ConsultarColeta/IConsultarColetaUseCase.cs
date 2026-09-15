namespace InvoiSys.Application.Coletas.ConsultarColeta;

public interface IConsultarColetaUseCase
{
    Task<ExecucaoColetaResponse> Execute(
        Guid id,
        CancellationToken cancellationToken);
}
