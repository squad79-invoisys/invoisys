using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class UnitOfWork(
    ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
