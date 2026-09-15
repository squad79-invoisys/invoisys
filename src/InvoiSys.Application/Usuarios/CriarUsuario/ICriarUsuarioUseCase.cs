using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Application.Usuarios.CriarUsuario;

public interface ICriarUsuarioUseCase
{
    Task<UserSummary> ExecutarAsync(
        CriarUsuarioRequest request,
        CancellationToken cancellationToken);
}
