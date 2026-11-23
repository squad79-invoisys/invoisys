using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class AnexoArquivadoRepository(
    ApplicationDbContext dbContext) : IAnexoArquivadoRepository
{
    private const string ViolacaoDeUnicidade = "23505";

    public Task<AnexoArquivado?> ObterPorUrlAsync(
        string url,
        CancellationToken cancellationToken) =>
        dbContext.AnexosArquivados
            .AsNoTracking()
            .FirstOrDefaultAsync(anexo => anexo.Url == url, cancellationToken);

    public async Task<bool> AdicionarAsync(
        AnexoArquivado anexo,
        CancellationToken cancellationToken)
    {
        dbContext.AnexosArquivados.Add(anexo);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == ViolacaoDeUnicidade)
        {
            // Dois downloads simultâneos do mesmo anexo: o primeiro gravou e
            // este perdeu a corrida. O objeto no bucket é o mesmo.
            dbContext.Entry(anexo).State = EntityState.Detached;
            return false;
        }
    }
}
