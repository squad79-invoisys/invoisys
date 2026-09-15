using InvoiSys.Application.Autenticacao.Login;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<Responses.ApiResponse<LoginResponse>> LoginAsync(
        [Body] LoginRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/auth/refresh")]
    Task<Responses.ApiResponse<LoginResponse>> RefreshAsync(
        CancellationToken cancellationToken = default);

    [Post("/api/auth/logout")]
    Task<Responses.ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default);
}
