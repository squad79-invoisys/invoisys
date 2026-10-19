using InvoiSys.Application.Dashboard.ObterResumo;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IDashboardClient
{
    // GET /api/dashboard/resumo: indicadores, 5 execuções recentes e 5 documentos recentes.
    [Get("/api/dashboard/resumo")]
    Task<Responses.ApiResponse<ResumoDashboardResponse>> ObterResumoAsync(
        CancellationToken cancellationToken = default);
}