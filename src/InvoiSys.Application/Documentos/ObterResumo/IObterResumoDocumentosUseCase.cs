namespace InvoiSys.Application.Documentos.ObterResumo;

public interface IObterResumoDocumentosUseCase
{
    Task<ResumoDocumentosResponse> Execute(CancellationToken cancellationToken);
}
