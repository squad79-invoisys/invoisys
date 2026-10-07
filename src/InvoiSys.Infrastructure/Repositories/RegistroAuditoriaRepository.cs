using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class RegistroAuditoriaRepository(
    ApplicationDbContext dbContext) : IRegistroAuditoriaRepository
{
    public Task AdicionarAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken) =>
        dbContext.RegistrosAuditoria.AddAsync(registro, cancellationToken).AsTask();

    public async Task<(IReadOnlyList<RegistroAuditoria> Itens, int Total)> ListarAsync(
        FiltroAuditoria filtro,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        var query = Filtrar(filtro);

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(registro => registro.OcorridoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<IReadOnlyDictionary<TipoEventoAuditoria, int>> ContarPorTipoAsync(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken)
    {
        var contagens = await Filtrar(filtro)
            .GroupBy(registro => registro.Tipo)
            .Select(grupo => new { Tipo = grupo.Key, Quantidade = grupo.Count() })
            .ToListAsync(cancellationToken);

        return contagens.ToDictionary(item => item.Tipo, item => item.Quantidade);
    }

    public async Task<IReadOnlyList<RegistroAuditoria>> ListarParaExportacaoAsync(
        FiltroAuditoria filtro,
        int limite,
        CancellationToken cancellationToken) =>
        await Filtrar(filtro)
            .OrderByDescending(registro => registro.OcorridoEm)
            .Take(limite)
            .ToListAsync(cancellationToken);

    private IQueryable<RegistroAuditoria> Filtrar(FiltroAuditoria filtro)
    {
        var query = dbContext.RegistrosAuditoria
            .AsNoTracking()
            .AsQueryable();

        if (filtro.Tipo is not null)
            query = query.Where(registro => registro.Tipo == filtro.Tipo);

        if (filtro.UsuarioId is not null)
            query = query.Where(registro => registro.UsuarioId == filtro.UsuarioId);

        if (filtro.De is not null)
            query = query.Where(registro => registro.OcorridoEm >= filtro.De);

        if (filtro.Ate is not null)
            query = query.Where(registro => registro.OcorridoEm <= filtro.Ate);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var padrao = $"%{EscaparLike(filtro.Busca)}%";
            query = query.Where(registro =>
                EF.Functions.ILike(registro.Atividade, padrao, "\\") ||
                EF.Functions.ILike(registro.Objeto, padrao, "\\") ||
                EF.Functions.ILike(registro.Responsavel, padrao, "\\"));
        }

        return query;
    }

    private static string EscaparLike(string valor) =>
        valor
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
}
