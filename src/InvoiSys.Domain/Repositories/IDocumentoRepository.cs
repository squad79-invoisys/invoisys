using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Repositories;

public interface IDocumentoRepository
{
    Task<int> InserirNovosAsync(
        IReadOnlyCollection<Documento> documentos,
        CancellationToken cancellationToken);

    Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        string? contexto,
        string? busca,
        CancellationToken cancellationToken);
}
