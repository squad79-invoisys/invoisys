using FluentValidation;
using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.CriarUsuario;

public sealed class CriarUsuarioUseCase(
    IValidator<CriarUsuarioRequest> validator,
    IUserManagementService userManagementService) : ICriarUsuarioUseCase
{
    public async Task<UserSummary> ExecutarAsync(
        CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        return await userManagementService.CriarAsync(
            request.Nome,
            request.Email,
            request.Senha,
            request.Perfil,
            cancellationToken);
    }
}
