using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Documentos;
using InvoiSys.Application.Documentos.ConsultarDocumento;
using InvoiSys.Application.Documentos.ListarDocumentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/documentos")]
[Authorize]
public sealed class DocumentosController(
    IConsultarDocumentoUseCase consultarDocumentoUseCase,
    IListarDocumentosUseCase listarDocumentosUseCase) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Consulta um documento",
        Description = "Retorna conteúdo, origem e metadados do documento coletado.",
        OperationId = "ConsultarDocumento",
        Tags = ["Documentos"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Documento consultado com sucesso.",
        typeof(ApiResponse<DocumentoResponse>))]
    public async Task<IActionResult> ConsultarPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await consultarDocumentoUseCase.Execute(
            id,
            cancellationToken);

        return Ok(ApiResponse<DocumentoResponse>.Ok(
            "Documento consultado com sucesso.",
            response));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista documentos",
        Description = "Retorna os documentos coletados de forma paginada.",
        OperationId = "ListarDocumentos",
        Tags = ["Documentos"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Documentos listados com sucesso.",
        typeof(ApiResponse<PagedResponse<DocumentoResponse>>))]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ListarDocumentosRequest request,
        CancellationToken cancellationToken)
    {
        var response = await listarDocumentosUseCase.Execute(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResponse<DocumentoResponse>>.Ok(
                "Documentos listados com sucesso.",
                response));
    }
}
