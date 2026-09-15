using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Documentos.ConsultarDocumento;

public sealed class ConsultarDocumentoUseCase(
    IDocumentoRepository documentoRepository) : IConsultarDocumentoUseCase
{
    public async Task<DocumentoResponse> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var documento = await documentoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Documento não encontrado.");

        return DocumentoResponse.FromEntity(documento);
    }
}
