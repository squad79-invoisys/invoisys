using InvoiSys.Application.Common.Responses;

namespace InvoiSys.Application.Historico.ListarHistorico;

public sealed record HistoricoResponse(
    PagedResponse<RegistroAuditoriaResponse> Eventos,
    TotaisHistoricoResponse Totais);

public sealed record TotaisHistoricoResponse(
    int Total,
    int Coletas,
    int Alteracoes,
    int Autenticacoes);
