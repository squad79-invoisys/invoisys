using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Historico.ExportarHistorico;
using InvoiSys.Application.Historico.ListarHistorico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/historico")]
[Authorize(Roles = "Administrador")]
public sealed class HistoricoController(
    IListarHistoricoUseCase listarHistoricoUseCase,
    IExportarHistoricoUseCase exportarHistoricoUseCase) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista o histórico de atividades",
        Description = "Retorna os eventos de auditoria com paginação, filtros e totais por categoria.",
        OperationId = "ListarHistorico",
        Tags = ["Histórico"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Histórico listado com sucesso.",
        typeof(ApiResponse<HistoricoResponse>))]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ListarHistoricoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await listarHistoricoUseCase.ExecutarAsync(
            request,
            cancellationToken);

        return Ok(ApiResponse<HistoricoResponse>.Ok(
            "Histórico listado com sucesso.",
            response));
    }

    [HttpGet("exportar")]
    [SwaggerOperation(
        Summary = "Exporta o histórico de atividades",
        Description = "Gera um arquivo CSV com os eventos do filtro aplicado (limitado aos 10.000 mais recentes).",
        OperationId = "ExportarHistorico",
        Tags = ["Histórico"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Arquivo CSV com os eventos.",
        typeof(FileContentResult),
        "text/csv")]
    public async Task<IActionResult> ExportarAsync(
        [FromQuery] ExportarHistoricoRequest request,
        CancellationToken cancellationToken)
    {
        var arquivo = await exportarHistoricoUseCase.ExecutarAsync(
            request,
            cancellationToken);

        return File(
            arquivo.Conteudo,
            arquivo.ContentType,
            arquivo.NomeArquivo);
    }
}
