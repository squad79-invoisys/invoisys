using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.AlterarStatusUsuario;

public sealed class AlterarStatusUsuarioUseCase(
    IUserManagementService userManagementService) : IAlterarStatusUsuarioUseCase
{
    public Task Execute(
        Guid id,
        AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken) =>
        userManagementService.DefinirStatusAsync(
            id,
            request.Ativo,
            cancellationToken);
}
