using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Dashboard.ObterResumo;

public sealed class ObterResumoDashboardUseCase(
    IDashboardRepository dashboardRepository) : IObterResumoDashboardUseCase
{
    private const int QuantidadeRecentes = 5;
    private const int JanelaHoras = 24;

    public async Task<ResumoDashboardResponse> Execute(
        CancellationToken cancellationToken)
    {
        var desde = DateTimeOffset.UtcNow.AddHours(-JanelaHoras);

        var fontesAtivas = await dashboardRepository.ContarFontesPorStatusAsync(
            StatusFonte.Ativa,
            cancellationToken);

        var fontesInativas = await dashboardRepository.ContarFontesPorStatusAsync(
            StatusFonte.Inativa,
            cancellationToken);

        var coletas = await dashboardRepository.ContarColetasDesdeAsync(
            desde,
            null,
            cancellationToken);

        var coletasComFalha = await dashboardRepository.ContarColetasDesdeAsync(
            desde,
            StatusExecucaoColeta.Falhou,
            cancellationToken);

        var totalDocumentos = await dashboardRepository.ContarDocumentosAsync(
            cancellationToken);

        var execucoesRecentes = await dashboardRepository.ListarExecucoesRecentesAsync(
            QuantidadeRecentes,
            cancellationToken);

        var documentosRecentes = await dashboardRepository.ListarDocumentosRecentesAsync(
            QuantidadeRecentes,
            cancellationToken);

        return new ResumoDashboardResponse(
            fontesAtivas,
            fontesInativas,
            coletas,
            coletasComFalha,
            totalDocumentos,
            execucoesRecentes.Select(ExecucaoRecenteResponse.FromReadModel).ToList(),
            documentosRecentes.Select(DocumentoRecenteResponse.FromEntity).ToList());
    }
}
