namespace InvoiSys.Application.Historico.ExportarHistorico;

public interface IExportarHistoricoUseCase
{
    Task<ArquivoExportado> ExecutarAsync(
        ExportarHistoricoRequest request,
        CancellationToken cancellationToken);
}
