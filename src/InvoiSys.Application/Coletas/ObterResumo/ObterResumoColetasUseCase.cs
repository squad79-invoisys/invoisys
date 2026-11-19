using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Coletas.ObterResumo;

public sealed class ObterResumoColetasUseCase(
    IDashboardRepository dashboardRepository) : IObterResumoColetasUseCase
{
    public async Task<ResumoColetasResponse> Execute(CancellationToken cancellationToken)
    {
        var porStatus = await dashboardRepository.ContarExecucoesPorStatusAsync(cancellationToken);
        var concluidas = porStatus.GetValueOrDefault(StatusExecucaoColeta.Concluida);
        var falhas = porStatus.GetValueOrDefault(StatusExecucaoColeta.Falhou);
        var emAndamento = porStatus.GetValueOrDefault(StatusExecucaoColeta.EmAndamento);

        return new ResumoColetasResponse(
            concluidas + falhas + emAndamento,
            concluidas,
            falhas,
            emAndamento);
    }
}
