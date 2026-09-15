namespace InvoiSys.Application.Usuarios.AlterarStatusUsuario;

public interface IAlterarStatusUsuarioUseCase
{
    Task Execute(
        Guid id,
        AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken);
}
