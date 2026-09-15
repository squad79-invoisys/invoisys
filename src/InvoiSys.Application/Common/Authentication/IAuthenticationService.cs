namespace InvoiSys.Application.Common.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        string email,
        string senha,
        CancellationToken cancellationToken);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
