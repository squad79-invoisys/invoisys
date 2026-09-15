using InvoiSys.Application.Common.Responses;

namespace InvoiSys.Application.Documentos.ListarDocumentos;

public interface IListarDocumentosUseCase
{
    Task<PagedResponse<DocumentoResponse>> Execute(
        ListarDocumentosRequest request,
        CancellationToken cancellationToken);
}
