using FluentValidation;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.ListarFontes;

public sealed class ListarFontesUseCase(
    IValidator<ListarFontesRequest> validator,
    IFonteRepository fonteRepository) : IListarFontesUseCase
{
    public async Task<PagedResponse<FonteResponse>> Execute(
        ListarFontesRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var (itens, total) = await fonteRepository.ListarAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.Tipo,
            request.Status,
            request.Busca,
            cancellationToken);

        return new PagedResponse<FonteResponse>(
            itens.Select(FonteResponse.FromEntity).ToList(),
            request.Pagina,
            request.TamanhoPagina,
            total);
    }
}
