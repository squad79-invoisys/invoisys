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

    [Get("/api/documentos")]
    Task<Responses.ApiResponse<Responses.PagedResponse<DocumentoResponse>>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 50,
        [Query] string? contexto = null,
        CancellationToken cancellationToken = default);
}
