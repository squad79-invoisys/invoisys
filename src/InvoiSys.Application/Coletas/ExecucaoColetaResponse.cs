using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Coletas;

public sealed record ExecucaoColetaResponse(
    Guid Id,
    Guid FonteId,
    DateTimeOffset Inicio,
    DateTimeOffset? Fim,
    StatusExecucaoColeta Status,
    TipoExecucao TipoExecucao,
    int QuantidadeDocumentos,
    string? MensagemErro,
    Guid? SolicitadaPorUsuarioId)
{
    public static ExecucaoColetaResponse FromEntity(ExecucaoColeta execucao) =>
        new(
            execucao.Id,
            execucao.FonteId,
            execucao.Inicio,
            execucao.Fim,
            execucao.Status,
            execucao.TipoExecucao,
            execucao.QuantidadeDocumentos,
            execucao.MensagemErro,
            execucao.SolicitadaPorUsuarioId);
}
