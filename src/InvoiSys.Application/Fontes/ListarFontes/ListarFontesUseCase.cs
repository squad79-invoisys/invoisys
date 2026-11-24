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
            request.Contexto,
            request.Busca,
            cancellationToken);

        var ultimasColetas = await fonteRepository.ObterUltimasColetasAsync(
            itens.Select(fonte => fonte.Id).ToList(),
            cancellationToken);

        return new PagedResponse<FonteResponse>(
            itens
                .Select(fonte => FonteResponse.FromEntity(
                    fonte,
                    ultimasColetas.TryGetValue(fonte.Id, out var ultima) ? ultima : null))
                .ToList(),
            request.Pagina,
            request.TamanhoPagina,
            total);
    }
}
