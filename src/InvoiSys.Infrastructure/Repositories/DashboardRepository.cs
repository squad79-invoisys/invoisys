using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class DashboardRepository(
    ApplicationDbContext dbContext) : IDashboardRepository
{
    public Task<int> ContarFontesPorStatusAsync(
        StatusFonte status,
        CancellationToken cancellationToken) =>
        dbContext.Fontes
            .AsNoTracking()
            .CountAsync(fonte => fonte.Status == status, cancellationToken);

    public Task<int> ContarColetasDesdeAsync(
        DateTimeOffset desde,
        StatusExecucaoColeta? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ExecucoesColeta
            .AsNoTracking()
            .Where(execucao => execucao.Inicio >= desde);

        if (status is not null)
            query = query.Where(execucao => execucao.Status == status);

        return query.CountAsync(cancellationToken);
    }

    public Task<int> ContarDocumentosAsync(CancellationToken cancellationToken) =>
        dbContext.Documentos
            .AsNoTracking()
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ExecucaoRecente>> ListarExecucoesRecentesAsync(
        int quantidade,
        CancellationToken cancellationToken)
    {
        var itens = await dbContext.ExecucoesColeta
            .AsNoTracking()
            .OrderByDescending(execucao => execucao.Inicio)
            .Take(quantidade)
            .Join(
                dbContext.Fontes.AsNoTracking(),
                execucao => execucao.FonteId,
                fonte => fonte.Id,
                (execucao, fonte) => new
                {
                    execucao.Id,
                    execucao.FonteId,
                    FonteNome = fonte.Nome,
                    execucao.Inicio,
                    execucao.Fim,
                    execucao.Status,
                    execucao.QuantidadeDocumentos
                })
            .ToListAsync(cancellationToken);

        return itens
            .Select(item => new ExecucaoRecente(
                item.Id,
                item.FonteId,
                item.FonteNome,
                item.Inicio,
                item.Fim,
                item.Status,
                item.QuantidadeDocumentos))
            .ToList();
    }

    public async Task<IReadOnlyList<Documento>> ListarDocumentosRecentesAsync(
        int quantidade,
        CancellationToken cancellationToken) =>
        await dbContext.Documentos
            .AsNoTracking()
            .OrderByDescending(documento => documento.DataColeta)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
}
