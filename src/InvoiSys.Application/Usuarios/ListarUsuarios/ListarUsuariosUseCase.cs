using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.ListarUsuarios;

public sealed class ListarUsuariosUseCase(
    IUserManagementService userManagementService) : IListarUsuariosUseCase
{
    public Task<IReadOnlyList<UserSummary>> Execute(
        CancellationToken cancellationToken) =>
        userManagementService.ListarAsync(cancellationToken);
}
