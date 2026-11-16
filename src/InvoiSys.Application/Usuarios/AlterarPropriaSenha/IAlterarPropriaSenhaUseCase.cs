namespace InvoiSys.Application.Usuarios.AlterarPropriaSenha;

public interface IAlterarPropriaSenhaUseCase
{
    Task Execute(
        AlterarPropriaSenhaRequest request,
        Guid? sessaoAtualId,
        CancellationToken cancellationToken);
}
