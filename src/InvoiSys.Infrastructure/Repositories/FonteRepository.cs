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
            .Where(fonte => fonte.Status == StatusFonte.Ativa && !fonte.AgendamentoPausado)
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

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> ObterUltimasColetasAsync(
        IReadOnlyCollection<Guid> fonteIds,
        CancellationToken cancellationToken)
    {
        if (fonteIds.Count == 0)
            return new Dictionary<Guid, DateTimeOffset>();

        var ids = fonteIds.Distinct().ToList();
        var ultimas = await dbContext.ExecucoesColeta
            .AsNoTracking()
            .Where(execucao => ids.Contains(execucao.FonteId))
            .GroupBy(execucao => execucao.FonteId)
            .Select(grupo => new
            {
                FonteId = grupo.Key,
                Ultima = grupo
                    .OrderByDescending(execucao => execucao.Inicio)
                    .ThenByDescending(execucao => execucao.Id)
                    .Select(execucao => execucao.Fim ?? execucao.Inicio)
                    .First()
            })
            .ToListAsync(cancellationToken);

        return ultimas.ToDictionary(item => item.FonteId, item => item.Ultima);
    }

    public async Task<int> IncrementarFalhasConsecutivasAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        await dbContext.Fontes
            .Where(fonte => fonte.Id == fonteId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                fonte => fonte.FalhasConsecutivas,
                fonte => fonte.FalhasConsecutivas + 1), cancellationToken);

        return await dbContext.Fontes
            .AsNoTracking()
            .Where(fonte => fonte.Id == fonteId)
            .Select(fonte => fonte.FalhasConsecutivas)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task ZerarFalhasConsecutivasAsync(
        Guid fonteId,
        CancellationToken cancellationToken) =>
        dbContext.Fontes
            .Where(fonte => fonte.Id == fonteId && fonte.FalhasConsecutivas != 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                fonte => fonte.FalhasConsecutivas, 0), cancellationToken);

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
