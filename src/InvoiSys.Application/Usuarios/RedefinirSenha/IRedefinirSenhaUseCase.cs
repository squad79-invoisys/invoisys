namespace InvoiSys.Application.Usuarios.RedefinirSenha;

public interface IRedefinirSenhaUseCase
{
    Task Execute(
        Guid id,
        RedefinirSenhaRequest request,
        CancellationToken cancellationToken);
}
