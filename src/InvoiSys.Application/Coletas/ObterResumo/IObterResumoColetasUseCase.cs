namespace InvoiSys.Application.Coletas.ObterResumo;

public interface IObterResumoColetasUseCase
{
    Task<ResumoColetasResponse> Execute(CancellationToken cancellationToken);
}
