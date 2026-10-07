using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Historico.ExportarHistorico;

public sealed record ExportarHistoricoRequest(
    TipoEventoAuditoria? Tipo = null,
    Guid? UsuarioId = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    string? Busca = null)
    : FiltroHistoricoRequest(Tipo, UsuarioId, De, Ate, Busca);
