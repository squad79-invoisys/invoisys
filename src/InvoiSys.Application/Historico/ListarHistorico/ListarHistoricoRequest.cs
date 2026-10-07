using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Historico.ListarHistorico;

public sealed record ListarHistoricoRequest(
    int Pagina = 1,
    int TamanhoPagina = 20,
    TipoEventoAuditoria? Tipo = null,
    Guid? UsuarioId = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    string? Busca = null)
    : FiltroHistoricoRequest(Tipo, UsuarioId, De, Ate, Busca);
