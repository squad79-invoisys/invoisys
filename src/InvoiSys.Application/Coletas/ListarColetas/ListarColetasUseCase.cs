using FluentValidation;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Coletas.ListarColetas;

public sealed class ListarColetasUseCase(
    IValidator<ListarColetasRequest> validator,
    IExecucaoColetaRepository execucaoRepository) : IListarColetasUseCase
{
    public async Task<PagedResponse<ExecucaoColetaResponse>> Execute(
        ListarColetasRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var (itens, total) = await execucaoRepository.ListarAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.FonteId,
            request.Status,
            cancellationToken);

        return new PagedResponse<ExecucaoColetaResponse>(
            itens.Select(ExecucaoColetaResponse.FromEntity).ToList(),
            request.Pagina,
            request.TamanhoPagina,
            total);
    }
}
