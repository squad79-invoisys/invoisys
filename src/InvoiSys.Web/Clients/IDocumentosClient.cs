using InvoiSys.Application.Documentos;
using InvoiSys.Application.Documentos.Anexos;
using InvoiSys.Application.Documentos.ObterResumo;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IDocumentosClient
{
    [Get("/api/documentos/resumo")]
    Task<Responses.ApiResponse<ResumoDocumentosResponse>> ObterResumoAsync(
        CancellationToken cancellationToken = default);

    // contexto e busca são filtros que a API já aceita (ListarDocumentosRequest).
    // Valores nulos não são enviados na query string.
    [Get("/api/documentos")]
    Task<Responses.ApiResponse<Responses.PagedResponse<DocumentoResponse>>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 50,
        [Query] string? contexto = null,
        [Query] string? busca = null,
        [Query] string? tipo = null,
        [Query(Format = "o")] DateTimeOffset? dataInicio = null,
        [Query(Format = "o")] DateTimeOffset? dataFim = null,
        CancellationToken cancellationToken = default);

    [Get("/api/documentos/{id}")]
    Task<Responses.ApiResponse<DocumentoResponse>> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/documentos/{id}/anexos")]
    Task<Responses.ApiResponse<AnexosDocumentoResponse>> ListarAnexosAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Devolve a resposta crua: o corpo é o arquivo, e só os erros trazem ApiResponse.
    [Get("/api/documentos/{id}/anexos/{anexoId}/arquivo")]
    Task<HttpResponseMessage> BaixarAnexoAsync(
        Guid id,
        string anexoId,
        CancellationToken cancellationToken = default);
}