using FluentValidation;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Documentos.ListarDocumentos;

public sealed class ListarDocumentosUseCase(
    IValidator<ListarDocumentosRequest> validator,
    IDocumentoRepository documentoRepository) : IListarDocumentosUseCase
{
    public async Task<PagedResponse<DocumentoResponse>> Execute(
        ListarDocumentosRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var (itens, total) = await documentoRepository.ListarAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.Busca,
            cancellationToken);

        return new PagedResponse<DocumentoResponse>(
            itens.Select(DocumentoResponse.FromEntity).ToList(),
            request.Pagina,
            request.TamanhoPagina,
            total);
    }
}
