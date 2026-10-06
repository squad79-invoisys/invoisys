using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Repositories;

public sealed record FiltroAuditoria(
    TipoEventoAuditoria? Tipo,
    Guid? UsuarioId,
    DateTimeOffset? De,
    DateTimeOffset? Ate,
    string? Busca);

public interface IRegistroAuditoriaRepository
{
    Task AdicionarAsync(RegistroAuditoria registro, CancellationToken cancellationToken);

    Task<(IReadOnlyList<RegistroAuditoria> Itens, int Total)> ListarAsync(
        FiltroAuditoria filtro,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<TipoEventoAuditoria, int>> ContarPorTipoAsync(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RegistroAuditoria>> ListarParaExportacaoAsync(
        FiltroAuditoria filtro,
        int limite,
        CancellationToken cancellationToken);
}
