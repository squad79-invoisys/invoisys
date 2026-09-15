using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class ExecucaoColetaRepository(
    ApplicationDbContext dbContext) : IExecucaoColetaRepository
{
    public Task AdicionarAsync(
        ExecucaoColeta execucao,
        CancellationToken cancellationToken) =>
        dbContext.ExecucoesColeta.AddAsync(execucao, cancellationToken).AsTask();

    public Task<ExecucaoColeta?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.ExecucoesColeta
            .AsNoTracking()
            .FirstOrDefaultAsync(
                execucao => execucao.Id == id,
                cancellationToken);

    public async Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        Guid? fonteId,
        StatusExecucaoColeta? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ExecucoesColeta
            .AsNoTracking()
            .AsQueryable();

        if (fonteId is not null)
            query = query.Where(execucao => execucao.FonteId == fonteId);

        if (status is not null)
            query = query.Where(execucao => execucao.Status == status);

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(execucao => execucao.Inicio)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }
}
