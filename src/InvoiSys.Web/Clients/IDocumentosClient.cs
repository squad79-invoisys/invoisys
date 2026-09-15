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
        CancellationToken cancellationToken = default);
}
