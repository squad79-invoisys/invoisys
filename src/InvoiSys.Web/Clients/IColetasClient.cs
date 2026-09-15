using InvoiSys.Application.Coletas;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IColetasClient
{
    [Get("/api/coletas")]
    Task<Responses.ApiResponse<Responses.PagedResponse<ExecucaoColetaResponse>>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 50,
        CancellationToken cancellationToken = default);

    [Post("/api/coletas/fontes/{fonteId}/executar")]
    Task<Responses.ApiResponse<ExecucaoColetaResponse>> ExecutarAsync(
        Guid fonteId,
        CancellationToken cancellationToken = default);
}
