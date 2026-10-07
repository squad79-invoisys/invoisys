using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Historico;

/// <summary>Filtros comuns à listagem e à exportação do histórico.</summary>
public abstract record FiltroHistoricoRequest(
    TipoEventoAuditoria? Tipo = null,
    Guid? UsuarioId = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    string? Busca = null)
{
    public FiltroAuditoria ParaFiltro() =>
        new(
            Tipo,
            UsuarioId,
            // O Npgsql só aceita DateTimeOffset em UTC para colunas timestamptz.
            De?.ToUniversalTime(),
            Ate?.ToUniversalTime(),
            string.IsNullOrWhiteSpace(Busca) ? null : Busca.Trim());
}
