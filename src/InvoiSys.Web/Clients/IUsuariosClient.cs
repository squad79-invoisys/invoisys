using InvoiSys.Application.Common.Authentication;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IUsuariosClient
{
    [Get("/api/usuarios")]
    Task<Responses.ApiResponse<IReadOnlyList<UserSummary>>> ListarAsync(
        CancellationToken cancellationToken = default);
}
