namespace InvoiSys.Application.Historico.ListarHistorico;

public interface IListarHistoricoUseCase
{
    Task<HistoricoResponse> ExecutarAsync(
        ListarHistoricoRequest request,
        CancellationToken cancellationToken);
}
