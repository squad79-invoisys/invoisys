using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class FonteRepository(
    ApplicationDbContext dbContext) : IFonteRepository
{
    public async Task<IReadOnlyList<Guid>> ListarIdsAtivasVencidasAsync(
        DateTimeOffset agoraUtc,
        CancellationToken cancellationToken)
    {
        var candidatas = await dbContext.Fontes
            .AsNoTracking()
            .Where(fonte => fonte.Status == StatusFonte.Ativa)
            .Select(fonte => new
            {
                fonte.Id,
                fonte.CriadoEm,
                fonte.PeriodicidadeMinutos,
                UltimaReferencia = dbContext.ExecucoesColeta
                    .Where(execucao => execucao.FonteId == fonte.Id)
                    .OrderByDescending(execucao => execucao.Inicio)
                    .ThenByDescending(execucao => execucao.Id)
                    .Select(execucao => (DateTimeOffset?)(execucao.Fim ?? execucao.Inicio))
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return candidatas
            .Where(fonte => agoraUtc >=
                (fonte.UltimaReferencia ?? fonte.CriadoEm)
                .AddMinutes(fonte.PeriodicidadeMinutos))
            .Select(fonte => fonte.Id)
            .ToList();
    }

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
        string? contexto,
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

        if (!string.IsNullOrWhiteSpace(contexto))
        {
            var contextoNormalizado = contexto.Trim();
            query = query.Where(fonte => fonte.Contexto == contextoNormalizado);
        }

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
