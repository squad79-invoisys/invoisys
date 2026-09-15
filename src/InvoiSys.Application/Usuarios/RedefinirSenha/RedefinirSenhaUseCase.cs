using FluentValidation;
using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.RedefinirSenha;

public sealed class RedefinirSenhaUseCase(
    IValidator<RedefinirSenhaRequest> validator,
    IUserManagementService userManagementService) : IRedefinirSenhaUseCase
{
    public async Task Execute(
        Guid id,
        RedefinirSenhaRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await userManagementService.RedefinirSenhaAsync(
            id,
            request.NovaSenha,
            cancellationToken);
    }
}
