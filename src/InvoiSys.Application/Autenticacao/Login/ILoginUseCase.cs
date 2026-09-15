namespace InvoiSys.Application.Autenticacao.Login;

public interface ILoginUseCase
{
    Task<AuthUseCaseResult> ExecutarAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}
