namespace InvoiSys.Application.Fontes.AlterarAgendamentoFonte;

public interface IAlterarAgendamentoFonteUseCase
{
    Task Execute(
        Guid id,
        bool pausado,
        CancellationToken cancellationToken);
}
