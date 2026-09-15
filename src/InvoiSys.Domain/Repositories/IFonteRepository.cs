using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Repositories;

public interface IFonteRepository
{
    Task AdicionarAsync(Fonte fonte, CancellationToken cancellationToken);
    Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        TipoFonte? tipo,
        StatusFonte? status,
        string? busca,
        CancellationToken cancellationToken);
}
