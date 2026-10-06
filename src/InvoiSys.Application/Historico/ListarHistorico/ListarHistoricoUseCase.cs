using FluentValidation;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Historico.ListarHistorico;

public sealed class ListarHistoricoUseCase(
    IValidator<ListarHistoricoRequest> validator,
    IRegistroAuditoriaRepository registroAuditoriaRepository) : IListarHistoricoUseCase
{
    public async Task<HistoricoResponse> ExecutarAsync(
        ListarHistoricoRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var filtro = request.ParaFiltro();

        var (itens, total) = await registroAuditoriaRepository.ListarAsync(
            filtro,
            request.Pagina,
            request.TamanhoPagina,
            cancellationToken);

        var porTipo = await registroAuditoriaRepository.ContarPorTipoAsync(
            filtro,
            cancellationToken);

        var eventos = new PagedResponse<RegistroAuditoriaResponse>(
            itens.Select(RegistroAuditoriaResponse.FromEntity).ToList(),
            request.Pagina,
            request.TamanhoPagina,
            total);

        var totais = new TotaisHistoricoResponse(
            total,
            Contar(porTipo, TipoEventoAuditoria.Coleta),
            Contar(porTipo, TipoEventoAuditoria.Fonte) + Contar(porTipo, TipoEventoAuditoria.Usuario),
            Contar(porTipo, TipoEventoAuditoria.Autenticacao));

        return new HistoricoResponse(eventos, totais);
    }

    private static int Contar(
        IReadOnlyDictionary<TipoEventoAuditoria, int> porTipo,
        TipoEventoAuditoria tipo) =>
        porTipo.TryGetValue(tipo, out var quantidade) ? quantidade : 0;
}
