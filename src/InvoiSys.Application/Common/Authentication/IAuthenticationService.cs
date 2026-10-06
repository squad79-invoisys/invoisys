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

    /// <summary>Revoga a sessão e retorna o usuário dono dela, ou null se a sessão não existir.</summary>
    Task<SessaoEncerrada?> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
