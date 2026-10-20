using InvoiSys.Application.Documentos;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IDocumentosClient
{
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
}
