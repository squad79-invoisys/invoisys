using InvoiSys.Application.Documentos;
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
        CancellationToken cancellationToken = default);
}