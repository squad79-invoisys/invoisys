using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class DocumentoRepository(
    ApplicationDbContext dbContext) : IDocumentoRepository
{
    public Task AdicionarVariosAsync(
        IEnumerable<Documento> documentos,
        CancellationToken cancellationToken) =>
        dbContext.Documentos.AddRangeAsync(documentos, cancellationToken);

    public Task<Documento?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                documento => documento.Id == id,
                cancellationToken);

    public async Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        string? busca,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Documentos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(documento =>
                EF.Functions.ILike(documento.Titulo, $"%{busca}%") ||
                EF.Functions.ILike(documento.Origem, $"%{busca}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(documento => documento.DataColeta)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }
}
