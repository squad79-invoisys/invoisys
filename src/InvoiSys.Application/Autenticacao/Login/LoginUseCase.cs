using FluentValidation;
using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Autenticacao.Login;

public sealed class LoginUseCase(
    IValidator<LoginRequest> validator,
    IAuthenticationService authenticationService) : ILoginUseCase
{
    public async Task<AuthUseCaseResult> ExecutarAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var resultado = await authenticationService.LoginAsync(
            request.Email,
            request.Senha,
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
