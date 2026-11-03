using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.ObterResumo;

public sealed class ObterResumoFontesUseCase(
    IDashboardRepository dashboardRepository) : IObterResumoFontesUseCase
{
    public async Task<ResumoFontesResponse> Execute(CancellationToken cancellationToken)
    {
        var ativas = await dashboardRepository.ContarFontesPorStatusAsync(
            StatusFonte.Ativa, cancellationToken);
        var inativas = await dashboardRepository.ContarFontesPorStatusAsync(
            StatusFonte.Inativa, cancellationToken);

        return new ResumoFontesResponse(ativas + inativas, ativas, inativas);
    }
}
