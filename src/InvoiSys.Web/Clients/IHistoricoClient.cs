using InvoiSys.Application.Historico.ListarHistorico;
using InvoiSys.Domain.Enums;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IHistoricoClient
{
    [Get("/api/historico")]
    Task<Responses.ApiResponse<HistoricoResponse>> ListarAsync(
        [Query] int pagina = 1,
        [Query] int tamanhoPagina = 20,
        [Query] TipoEventoAuditoria? tipo = null,
        [Query] Guid? usuarioId = null,
        [Query(Format = "o")] DateTimeOffset? de = null,
        [Query(Format = "o")] DateTimeOffset? ate = null,
        [Query] string? busca = null,
        CancellationToken cancellationToken = default);

    [Get("/api/historico/exportar")]
    Task<HttpResponseMessage> ExportarAsync(
        [Query] TipoEventoAuditoria? tipo = null,
        [Query] Guid? usuarioId = null,
        [Query(Format = "o")] DateTimeOffset? de = null,
        [Query(Format = "o")] DateTimeOffset? ate = null,
        [Query] string? busca = null,
        CancellationToken cancellationToken = default);
}
