using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Dashboard.ObterResumo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(
    IObterResumoDashboardUseCase obterResumoDashboardUseCase) : ControllerBase
{
    [HttpGet("resumo")]
    [SwaggerOperation(
        Summary = "Obtém o resumo da operação",
        Description = "Retorna os indicadores das fontes, coletas e documentos, " +
                      "além das execuções e dos documentos mais recentes.",
        OperationId = "ObterResumoDashboard",
        Tags = ["Dashboard"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Resumo obtido com sucesso.",
        typeof(ApiResponse<ResumoDashboardResponse>))]
    [SwaggerResponse(
        StatusCodes.Status401Unauthorized,
        "Requisição sem autenticação válida.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> ObterResumoAsync(
        CancellationToken cancellationToken)
    {
        var response = await obterResumoDashboardUseCase.Execute(cancellationToken);

        return Ok(ApiResponse<ResumoDashboardResponse>.Ok(
            "Resumo obtido com sucesso.",
            response));
    }
}
