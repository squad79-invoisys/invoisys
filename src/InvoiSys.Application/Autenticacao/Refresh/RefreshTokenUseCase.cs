using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Autenticacao.Refresh;

public sealed class RefreshTokenUseCase(
    IAuthenticationService authenticationService) : IRefreshTokenUseCase
{
    public async Task<AuthUseCaseResult> ExecutarAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var resultado = await authenticationService.RefreshAsync(
            refreshToken,
            cancellationToken);

        return new AuthUseCaseResult(
            new LoginResponse(
                resultado.AccessToken,
                resultado.AccessTokenExpiraEm,
                resultado.Usuario.Id,
                resultado.Usuario.Nome,
                resultado.Usuario.Email,
                resultado.Usuario.Perfis),
            resultado.RefreshToken,
            resultado.RefreshTokenExpiraEm);
    }
}
