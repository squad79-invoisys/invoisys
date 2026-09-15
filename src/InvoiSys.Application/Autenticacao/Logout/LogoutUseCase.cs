using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Autenticacao.Logout;

public sealed class LogoutUseCase(
    IAuthenticationService authenticationService) : ILogoutUseCase
{
    public Task ExecutarAsync(
        string refreshToken,
        CancellationToken cancellationToken) =>
        authenticationService.LogoutAsync(refreshToken, cancellationToken);
}
