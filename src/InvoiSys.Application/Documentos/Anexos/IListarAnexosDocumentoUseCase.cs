namespace InvoiSys.Application.Documentos.Anexos;

public interface IListarAnexosDocumentoUseCase
{
    Task<AnexosDocumentoResponse> Execute(
        Guid documentoId,
        CancellationToken cancellationToken);
}
