using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Usuarios.AlterarStatusUsuario;

public sealed class AlterarStatusUsuarioUseCase(
    IUserManagementService userManagementService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : IAlterarStatusUsuarioUseCase
{
    public async Task Execute(
        Guid id,
        AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await userManagementService.DefinirStatusAsync(
            id,
            request.Ativo,
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Usuario,
            request.Ativo ? "Usuário ativado" : "Usuário desativado",
            usuario.Email,
            usuario.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
