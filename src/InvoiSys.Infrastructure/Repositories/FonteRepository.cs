using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class FonteRepository(
    ApplicationDbContext dbContext) : IFonteRepository
{
    public Task AdicionarAsync(
        Fonte fonte,
        CancellationToken cancellationToken) =>
        dbContext.Fontes.AddAsync(fonte, cancellationToken).AsTask();

    public Task<Fonte?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.Fontes.FirstOrDefaultAsync(
            fonte => fonte.Id == id,
            cancellationToken);

    public async Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        TipoFonte? tipo,
        StatusFonte? status,
        string? busca,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Fontes
            .AsNoTracking()
            .AsQueryable();

        if (tipo is not null)
            query = query.Where(fonte => fonte.Tipo == tipo);

        if (status is not null)
            query = query.Where(fonte => fonte.Status == status);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(fonte =>
                EF.Functions.ILike(fonte.Nome, $"%{busca}%") ||
                EF.Functions.ILike(fonte.Url, $"%{busca}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderBy(fonte => fonte.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }
}
