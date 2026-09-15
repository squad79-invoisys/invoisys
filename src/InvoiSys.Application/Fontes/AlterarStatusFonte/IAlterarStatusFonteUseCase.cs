namespace InvoiSys.Application.Fontes.AlterarStatusFonte;

public interface IAlterarStatusFonteUseCase
{
    Task Execute(
        Guid id,
        bool ativa,
        CancellationToken cancellationToken);
}
