using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Documentos.Anexos;

public sealed class ListarAnexosDocumentoUseCase(
    IDocumentoRepository documentoRepository,
    AnexoCatalogo catalogo) : IListarAnexosDocumentoUseCase
{
    public async Task<AnexosDocumentoResponse> Execute(
        Guid documentoId,
        CancellationToken cancellationToken)
    {
        var documento = await documentoRepository.ObterPorIdAsync(documentoId, cancellationToken)
            ?? throw new NotFoundException("Documento não encontrado.");

        return await catalogo.ListarAsync(documento, cancellationToken);
    }
}
