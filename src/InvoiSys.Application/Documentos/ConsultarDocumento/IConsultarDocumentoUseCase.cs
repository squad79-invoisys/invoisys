namespace InvoiSys.Application.Documentos.ConsultarDocumento;

public interface IConsultarDocumentoUseCase
{
    Task<DocumentoResponse> Execute(
        Guid id,
        CancellationToken cancellationToken);
}
