using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Documentos;
using InvoiSys.Application.Documentos.Anexos;
using InvoiSys.Application.Documentos.ConsultarDocumento;
using InvoiSys.Application.Documentos.ListarDocumentos;
using InvoiSys.Application.Documentos.ObterResumo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/documentos")]
[Authorize]
public sealed class DocumentosController(
    IConsultarDocumentoUseCase consultarDocumentoUseCase,
    IListarDocumentosUseCase listarDocumentosUseCase,
    IObterResumoDocumentosUseCase obterResumoDocumentosUseCase,
    IListarAnexosDocumentoUseCase listarAnexosDocumentoUseCase,
    IBaixarAnexoDocumentoUseCase baixarAnexoDocumentoUseCase) : ControllerBase
{
    [HttpGet("resumo")]
    [SwaggerOperation(
        Summary = "Obtém o resumo dos documentos",
        Description = "Retorna o total global de documentos cadastrados.",
        OperationId = "ObterResumoDocumentos",
        Tags = ["Documentos"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Resumo obtido com sucesso.",
        typeof(ApiResponse<ResumoDocumentosResponse>))]
    public async Task<IActionResult> ObterResumoAsync(CancellationToken cancellationToken)
    {
        var response = await obterResumoDocumentosUseCase.Execute(cancellationToken);

        return Ok(ApiResponse<ResumoDocumentosResponse>.Ok(
            "Resumo obtido com sucesso.", response));
    }

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
        CancellationToken cancellationToken
        )
    {
        var response = await consultarDocumentoUseCase.Execute(
            id,
            cancellationToken);

        return Ok(ApiResponse<DocumentoResponse>.Ok(
            "Documento consultado com sucesso.",
            response));
    }

    [HttpGet("{id:guid}/anexos")]
    [SwaggerOperation(
        Summary = "Lista os anexos de um documento",
        Description = "Retorna os arquivos (PDF, planilhas, imagens etc.) declarados pela fonte ou " +
                      "encontrados na página de origem do documento.",
        OperationId = "ListarAnexosDocumento",
        Tags = ["Documentos"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Anexos listados com sucesso.",
        typeof(ApiResponse<AnexosDocumentoResponse>))]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "Documento não encontrado.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> ListarAnexosAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await listarAnexosDocumentoUseCase.Execute(id, cancellationToken);

        return Ok(ApiResponse<AnexosDocumentoResponse>.Ok(
            "Anexos listados com sucesso.",
            response));
    }

    [HttpGet("{id:guid}/anexos/{anexoId:regex(^[[0-9a-fA-F]]{{16}}$)}/arquivo")]
    [SwaggerOperation(
        Summary = "Baixa um anexo do documento",
        Description = "Entrega o arquivo do anexo, buscado no site de origem. O identificador " +
                      "vem da listagem de anexos do próprio documento.",
        OperationId = "BaixarAnexoDocumento",
        Tags = ["Documentos"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Conteúdo do arquivo.", typeof(FileStreamResult))]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "Documento ou anexo não encontrado.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> BaixarAnexoAsync(
        Guid id,
        string anexoId,
        CancellationToken cancellationToken)
    {
        var arquivo = await baixarAnexoDocumentoUseCase.Execute(id, anexoId, cancellationToken);

        // O tipo é definido pela API (nunca repassado do site de origem) e o
        // nosniff impede que o navegador o reinterprete.
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // O FileStreamResult descarta o fluxo ao terminar, o que encerra a
        // conexão com a origem.
        return File(arquivo.Conteudo, arquivo.TipoConteudo, arquivo.NomeArquivo);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista documentos",
        Description = "Retorna os documentos coletados de forma paginada, com filtros opcionais por busca (título ou origem), tipo, contexto e período da data de coleta.",
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
