namespace InvoiSys.Application.Autenticacao.Logout;

public interface ILogoutUseCase
{
    Task ExecutarAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
