using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class UnitOfWork(
    ApplicationDbContext dbContext) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Já existe uma transação em andamento.");

        _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Não existe uma transação em andamento.");

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_transaction is not null)
                await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_transaction is not null)
                await _transaction.DisposeAsync();

            _transaction = null;
            dbContext.ChangeTracker.Clear();
        }
    }
}
