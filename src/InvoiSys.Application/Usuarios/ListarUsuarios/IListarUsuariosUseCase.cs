using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.ListarUsuarios;

public interface IListarUsuariosUseCase
{
    Task<IReadOnlyList<UserSummary>> Execute(
        CancellationToken cancellationToken);
}
