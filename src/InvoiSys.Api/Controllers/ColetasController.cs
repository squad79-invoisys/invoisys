using InvoiSys.Application.Coletas;
using InvoiSys.Application.Coletas.ConsultarColeta;
using InvoiSys.Application.Coletas.ExecutarColetaManual;
using InvoiSys.Application.Coletas.ListarColetas;
using InvoiSys.Application.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/coletas")]
[Authorize]
public sealed class ColetasController(
    IExecutarColetaManualUseCase executarColetaManualUseCase,
    IConsultarColetaUseCase consultarColetaUseCase,
    IListarColetasUseCase listarColetasUseCase) : ControllerBase
{
    [HttpPost("fontes/{fonteId:guid}/executar")]
    [Authorize(Roles = "Administrador,Operador")]
    [SwaggerOperation(
        Summary = "Executa uma coleta manual",
        Description = "Executa imediatamente o coletor configurado para a fonte.",
        OperationId = "ExecutarColetaManual",
        Tags = ["Coletas"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Coleta executada com sucesso.",
        typeof(ApiResponse<ExecucaoColetaResponse>))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "A coleta não pôde ser executada.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> ExecutarManualAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        var response = await executarColetaManualUseCase.ExecutarAsync(
            fonteId,
            cancellationToken);

        return Ok(ApiResponse<ExecucaoColetaResponse>.Ok(
            "Coleta executada com sucesso.",
            response));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Consulta uma execução de coleta",
        Description = "Retorna os dados de uma execução específica.",
        OperationId = "ConsultarColeta",
        Tags = ["Coletas"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Coleta consultada com sucesso.",
        typeof(ApiResponse<ExecucaoColetaResponse>))]
    public async Task<IActionResult> ConsultarPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await consultarColetaUseCase.Execute(
            id,
            cancellationToken);

        return Ok(ApiResponse<ExecucaoColetaResponse>.Ok(
            "Coleta consultada com sucesso.",
            response));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista o histórico de coletas",
        Description = "Retorna as execuções com paginação e filtros.",
        OperationId = "ListarColetas",
        Tags = ["Coletas"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Coletas listadas com sucesso.",
        typeof(ApiResponse<PagedResponse<ExecucaoColetaResponse>>))]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ListarColetasRequest request,
        CancellationToken cancellationToken)
    {
        var response = await listarColetasUseCase.Execute(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResponse<ExecucaoColetaResponse>>.Ok(
                "Coletas listadas com sucesso.",
                response));
    }
}
