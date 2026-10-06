using FluentValidation;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Autenticacao.Login;

public sealed class LoginUseCase(
    IValidator<LoginRequest> validator,
    IAuthenticationService authenticationService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : ILoginUseCase
{
    public async Task<AuthUseCaseResult> ExecutarAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        AuthenticationResult resultado;

        try
        {
            resultado = await authenticationService.LoginAsync(
                request.Email,
                request.Senha,
                cancellationToken);
        }
        catch (UnauthorizedException)
        {
            var email = request.Email.Trim();
            await auditoriaService.RegistrarAsync(
                TipoEventoAuditoria.Autenticacao,
                "Tentativa de login recusada",
                email,
                null,
                null,
                email,
                cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            throw;
        }

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Autenticacao,
            "Login realizado",
            resultado.Usuario.Email,
            resultado.Usuario.Id,
            resultado.Usuario.Id,
            resultado.Usuario.Email,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

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
