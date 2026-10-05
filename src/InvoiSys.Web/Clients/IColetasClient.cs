using InvoiSys.Application.Coletas;
using InvoiSys.Domain.Enums;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IColetasClient
{
    // fonteId e status são filtros que a API já aceita (ListarColetasRequest).
    // Valores nulos não são enviados na query string.
    [Get("/api/coletas")]
    Task<Responses.ApiResponse<Responses.PagedResponse<ExecucaoColetaResponse>>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 50,
        [Query] Guid? fonteId = null,
        [Query] StatusExecucaoColeta? status = null,
        CancellationToken cancellationToken = default);

    [Post("/api/coletas/fontes/{fonteId}/executar")]
    Task<Responses.ApiResponse<ExecucaoColetaResponse>> ExecutarAsync(
        Guid fonteId,
        CancellationToken cancellationToken = default);
}