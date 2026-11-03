using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Documentos.ObterResumo;

public sealed class ObterResumoDocumentosUseCase(
    IDashboardRepository dashboardRepository) : IObterResumoDocumentosUseCase
{
    public async Task<ResumoDocumentosResponse> Execute(CancellationToken cancellationToken) =>
        new(await dashboardRepository.ContarDocumentosAsync(cancellationToken));
}
