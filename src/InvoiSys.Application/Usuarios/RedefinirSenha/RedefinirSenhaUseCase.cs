using FluentValidation;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Usuarios.RedefinirSenha;

public sealed class RedefinirSenhaUseCase(
    IValidator<RedefinirSenhaRequest> validator,
    IUserManagementService userManagementService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : IRedefinirSenhaUseCase
{
    public async Task Execute(
        Guid id,
        RedefinirSenhaRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManagementService.RedefinirSenhaAsync(
            id,
            request.NovaSenha,
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Usuario,
            "Senha redefinida",
            usuario.Email,
            usuario.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
