namespace InvoiSys.Application.Dashboard.ObterResumo;

public interface IObterResumoDashboardUseCase
{
    Task<ResumoDashboardResponse> Execute(CancellationToken cancellationToken);
}
