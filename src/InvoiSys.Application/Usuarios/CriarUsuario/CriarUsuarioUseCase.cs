using FluentValidation;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Usuarios.CriarUsuario;

public sealed class CriarUsuarioUseCase(
    IValidator<CriarUsuarioRequest> validator,
    IUserManagementService userManagementService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : ICriarUsuarioUseCase
{
    public async Task<UserSummary> ExecutarAsync(
        CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManagementService.CriarAsync(
            request.Nome,
            request.Email,
            request.Senha,
            request.Perfil,
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Usuario,
            "Usuário criado",
            usuario.Email,
            usuario.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return usuario;
    }
}
