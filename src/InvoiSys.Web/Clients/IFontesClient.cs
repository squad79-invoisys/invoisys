using InvoiSys.Application.Fontes;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Application.Fontes.CriarFonte;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IFontesClient
{
    [Get("/api/fontes")]
    Task<Responses.ApiResponse<Responses.PagedResponse<FonteResponse>>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 50,
        CancellationToken cancellationToken = default);

    [Post("/api/fontes")]
    Task<Responses.ApiResponse<FonteResponse>> CriarAsync(
        [Body] CriarFonteRequest request,
        CancellationToken cancellationToken = default);

    [Put("/api/fontes/{id}")]
    Task<Responses.ApiResponse<FonteResponse>> AtualizarAsync(
        Guid id,
        [Body] AtualizarFonteRequest request,
        CancellationToken cancellationToken = default);

    [Patch("/api/fontes/{id}/ativar")]
    Task<Responses.ApiResponse<object>> AtivarAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Patch("/api/fontes/{id}/desativar")]
    Task<Responses.ApiResponse<object>> DesativarAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
