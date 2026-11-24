using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Repositories;

public interface IFonteRepository
{
    Task<IReadOnlyList<Guid>> ListarIdsAtivasVencidasAsync(
        DateTimeOffset agoraUtc,
        CancellationToken cancellationToken);
    Task AdicionarAsync(Fonte fonte, CancellationToken cancellationToken);

    /// <summary>
    /// Retorna, por fonte, o fim (ou o início, se ainda em andamento) da execução mais recente.
    /// Fontes sem execução não aparecem no resultado.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> ObterUltimasColetasAsync(
        IReadOnlyCollection<Guid> fonteIds,
        CancellationToken cancellationToken);

    /// <summary>Incrementa atomicamente o contador e devolve o novo valor.</summary>
    Task<int> IncrementarFalhasConsecutivasAsync(Guid fonteId, CancellationToken cancellationToken);

    /// <summary>Zera atomicamente o contador de falhas consecutivas.</summary>
    Task ZerarFalhasConsecutivasAsync(Guid fonteId, CancellationToken cancellationToken);
    Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        TipoFonte? tipo,
        StatusFonte? status,
        string? contexto,
        string? busca,
        CancellationToken cancellationToken);
}
