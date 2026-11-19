using InvoiSys.Domain.Repositories;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class UnitOfWork(
    ApplicationDbContext dbContext,
    IFonteColetaLock? fonteColetaLock = null,
    ILogger<UnitOfWork>? logger = null) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Já existe uma transação em andamento.");

        _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (fonteColetaLock?.PosseAtual is { } posse)
                await posse.ValidarTransacaoAsync(_transaction.GetDbTransaction(), cancellationToken);
        }
        catch (Exception validationException)
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await RollbackTransactionAsync(cleanup.Token);
            }
            catch (Exception rollbackException)
            {
                RegistrarFalhaLimpeza("rollback", rollbackException);
                try
                {
                    var connection = dbContext.Database.GetDbConnection();
                    if (connection is NpgsqlConnection npgsqlConnection)
                    {
                        try { NpgsqlConnection.ClearPool(npgsqlConnection); }
                        catch (Exception poolException)
                        {
                            RegistrarFalhaLimpeza("invalidação do pool", poolException);
                        }
                    }

                    try { await dbContext.Database.CloseConnectionAsync(); }
                    catch (Exception closeException)
                    {
                        RegistrarFalhaLimpeza("fechamento via EF Core", closeException);
                    }
                    await connection.CloseAsync();
                }
                catch (Exception closeException)
                {
                    RegistrarFalhaLimpeza("fechamento da conexão", closeException);
                }
            }
            finally
            {
                _transaction = null;
                try { dbContext.ChangeTracker.Clear(); }
                catch (Exception trackerException)
                {
                    RegistrarFalhaLimpeza("limpeza do tracker", trackerException);
                }
            }
            throw;

            void RegistrarFalhaLimpeza(string etapa, Exception exception)
            {
                validationException.Data[$"UnitOfWork.{etapa}"] = exception;
                try
                {
                    logger?.LogError(exception,
                        "Falha na {Etapa} após erro na validação inicial da posse.", etapa);
                }
                catch
                {
                    // Uma falha no logger não pode ocultar a exceção da validação.
                }
            }
        }
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        if (fonteColetaLock?.PosseAtual is { } posse)
        {
            if (_transaction is null)
                throw new InvalidOperationException("A escrita de coleta exige uma transação validada.");
            await posse.VerificarSessaoAsync(cancellationToken);
        }
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Não existe uma transação em andamento.");

        if (fonteColetaLock?.PosseAtual is { } posse)
            await posse.RenovarTransacaoAsync(_transaction.GetDbTransaction(), cancellationToken);
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
