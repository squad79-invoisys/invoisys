namespace InvoiSys.Application.Autenticacao.Refresh;

public interface IRefreshTokenUseCase
{
    Task<AuthUseCaseResult> ExecutarAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
