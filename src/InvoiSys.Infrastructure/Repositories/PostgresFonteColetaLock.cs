using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class PostgresFonteColetaLock(
    ApplicationDbContext dbContext,
    ILogger<PostgresFonteColetaLock> logger) : IFonteColetaLock
{
    private Lease? _lease;
    public IFonteColetaLease? PosseAtual => _lease is { EstaReivindicada: true } ? _lease : null;

    public async Task<IFonteColetaLease?> TentarAdquirirAsync(Guid fonteId, CancellationToken cancellationToken)
    {
        if (_lease is not null)
            throw new InvalidOperationException("O escopo já possui um lock de coleta.");

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var chave = Chave(fonteId);
        try
        {
            await using var command = new NpgsqlCommand(
                "SELECT pg_backend_pid(), pg_try_advisory_lock(@chave)", connection);
            command.Parameters.AddWithValue("chave", chave);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("O PostgreSQL não respondeu à aquisição do lock.");
            var sessao = reader.GetInt32(0);
            var adquirido = reader.GetBoolean(1);
            await reader.DisposeAsync();
            if (!adquirido)
            {
                await dbContext.Database.CloseConnectionAsync();
                return null;
            }

            _lease = new Lease(dbContext, connection, fonteId, chave, sessao, logger,
                () => _lease = null);
            return _lease;
        }
        catch
        {
            if (_lease is null)
            {
                NpgsqlConnection.ClearPool(connection);
                await dbContext.Database.CloseConnectionAsync();
            }
            throw;
        }
    }

    public Task VerificarSessaoAsync(CancellationToken cancellationToken) =>
        _lease?.VerificarSessaoAsync(cancellationToken) ?? Task.CompletedTask;

    private static long Chave(Guid fonteId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"InvoiSys:FonteColeta:{fonteId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    private sealed class Lease(
        ApplicationDbContext dbContext,
        NpgsqlConnection connection,
        Guid fonteId,
        long chave,
        int sessao,
        ILogger logger,
        Action onDispose) : IFonteColetaLease
    {
        private const string AdvisoryHeld = """
            EXISTS (SELECT 1 FROM pg_locks
                WHERE locktype = 'advisory' AND pid = pg_backend_pid() AND granted
                  AND objsubid = 1
                  AND classid::bigint = ((@chave >> 32) & 4294967295)
                  AND objid::bigint = (@chave & 4294967295))
            """;
        private readonly Guid _proprietarioId = Guid.NewGuid();
        private long _geracao;
        private bool _disposed;
        private bool _lost;
        public bool EstaReivindicada { get; private set; }

        public async Task<bool> TentarReivindicarAsync(CancellationToken cancellationToken)
        {
            if (EstaReivindicada)
                throw new InvalidOperationException("A posse já foi reivindicada.");
            await VerificarSessaoAsync(cancellationToken);
            try
            {
                await using var command = CriarComando($"""
                    INSERT INTO fontes_coleta_lease (fonte_id, proprietario_id, geracao, valido_ate)
                    SELECT @fonte, @proprietario, 1, clock_timestamp() + interval '60 seconds'
                    WHERE pg_backend_pid() = @sessao AND {AdvisoryHeld}
                    ON CONFLICT (fonte_id) DO UPDATE
                    SET proprietario_id = EXCLUDED.proprietario_id,
                        geracao = fontes_coleta_lease.geracao + 1,
                        valido_ate = clock_timestamp() + interval '60 seconds'
                    WHERE fontes_coleta_lease.valido_ate <= clock_timestamp()
                      AND pg_backend_pid() = @sessao AND {AdvisoryHeld}
                    RETURNING geracao
                    """);
                var result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is null)
                {
                    await VerificarSessaoAsync(cancellationToken);
                    return false;
                }
                _geracao = (long)result;
                EstaReivindicada = true;
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { PerderPosse(); throw new ColetaPossePerdidaException("Falha ao reivindicar a posse da Fonte.", exception); }
        }

        public async Task RenovarAsync(CancellationToken cancellationToken)
        {
            ExigirPosse();
            try
            {
                await using var command = CriarComando($"""
                    UPDATE fontes_coleta_lease SET valido_ate = clock_timestamp() + interval '60 seconds'
                    WHERE fonte_id = @fonte AND proprietario_id = @proprietario
                      AND geracao = @geracao AND valido_ate > clock_timestamp()
                      AND pg_backend_pid() = @sessao AND {AdvisoryHeld}
                    RETURNING 1
                    """);
                command.CommandTimeout = 3;
                if (await command.ExecuteScalarAsync(cancellationToken) is null)
                    throw new ColetaPossePerdidaException("A posse da Fonte expirou ou mudou.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { PerderPosse(); throw new ColetaPossePerdidaException("Falha ao renovar a posse da Fonte.", exception); }
        }

        public async Task ValidarTransacaoAsync(DbTransaction transaction, CancellationToken cancellationToken)
        {
            ExigirTransacao(transaction);
            try
            {
                await using var command = CriarComando($"""
                    SELECT proprietario_id = @proprietario AND geracao = @geracao
                       AND valido_ate > clock_timestamp() AND pg_backend_pid() = @sessao
                       AND {AdvisoryHeld}
                    FROM fontes_coleta_lease WHERE fonte_id = @fonte FOR UPDATE
                    """, transaction);
                if (await command.ExecuteScalarAsync(cancellationToken) is not true)
                    throw new ColetaPossePerdidaException("A posse da Fonte não é válida na transação.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { PerderPosse(); throw new ColetaPossePerdidaException("Falha ao validar a posse na transação.", exception); }
        }

        public async Task RenovarTransacaoAsync(DbTransaction transaction, CancellationToken cancellationToken)
        {
            ExigirTransacao(transaction);
            try
            {
                await using var command = CriarComando($"""
                    UPDATE fontes_coleta_lease SET valido_ate = clock_timestamp() + interval '60 seconds'
                    WHERE fonte_id = @fonte AND proprietario_id = @proprietario
                      AND geracao = @geracao AND valido_ate > clock_timestamp()
                      AND pg_backend_pid() = @sessao AND {AdvisoryHeld}
                    RETURNING 1
                    """, transaction);
                if (await command.ExecuteScalarAsync(cancellationToken) is null)
                    throw new ColetaPossePerdidaException("A posse expirou antes do commit.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { PerderPosse(); throw new ColetaPossePerdidaException("Falha ao renovar a posse antes do commit.", exception); }
        }

        public async Task VerificarSessaoAsync(CancellationToken cancellationToken)
        {
            if (_disposed || _lost || connection.State != ConnectionState.Open)
                throw new ColetaPossePerdidaException("A sessão que detém a coleta foi perdida.");
            try
            {
                await using var command = CriarComando($"SELECT pg_backend_pid() = @sessao AND {AdvisoryHeld}");
                if (await command.ExecuteScalarAsync(cancellationToken) is not true)
                    throw new ColetaPossePerdidaException("A sessão ou o advisory lock da Fonte mudou.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { PerderPosse(); throw new ColetaPossePerdidaException("Falha ao verificar a sessão de coleta.", exception); }
        }

        private NpgsqlCommand CriarComando(string sql, DbTransaction? transaction = null)
        {
            var command = new NpgsqlCommand(sql, connection, transaction as NpgsqlTransaction);
            command.Parameters.AddWithValue("fonte", fonteId);
            command.Parameters.AddWithValue("proprietario", _proprietarioId);
            command.Parameters.AddWithValue("geracao", _geracao);
            command.Parameters.AddWithValue("sessao", sessao);
            command.Parameters.AddWithValue("chave", chave);
            return command;
        }

        private void ExigirPosse()
        {
            if (_disposed || _lost || !EstaReivindicada || connection.State != ConnectionState.Open)
                throw new ColetaPossePerdidaException("A posse da Fonte foi perdida.");
        }

        private void ExigirTransacao(DbTransaction transaction)
        {
            ExigirPosse();
            if (!ReferenceEquals(transaction.Connection, connection))
                throw new ColetaPossePerdidaException("A transação não usa a sessão original da Fonte.");
        }

        private void PerderPosse()
        {
            _lost = true;
            NpgsqlConnection.ClearPool(connection);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                if (EstaReivindicada)
                {
                    if (_lost || connection.State != ConnectionState.Open)
                        logger.LogWarning("Liberação do lease da Fonte {FonteId}, proprietário {ProprietarioId}, geração {Geracao}, não confirmada: sessão perdida.", fonteId, _proprietarioId, _geracao);
                    else
                    {
                        try
                        {
                            await using var release = CriarComando("""
                                UPDATE fontes_coleta_lease SET valido_ate = clock_timestamp()
                                WHERE fonte_id = @fonte AND proprietario_id = @proprietario
                                  AND geracao = @geracao AND pg_backend_pid() = @sessao
                                RETURNING 1
                                """);
                            release.CommandTimeout = 3;
                            if (await release.ExecuteScalarAsync(cleanup.Token) is null)
                                logger.LogWarning("Liberação do lease da Fonte {FonteId}, proprietário {ProprietarioId}, geração {Geracao}, não confirmada: posse diferente.", fonteId, _proprietarioId, _geracao);
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(exception, "Liberação do lease da Fonte {FonteId}, proprietário {ProprietarioId}, geração {Geracao}, não confirmada.", fonteId, _proprietarioId, _geracao);
                            PerderPosse();
                        }
                    }
                }
                if (!_lost && connection.State == ConnectionState.Open)
                {
                    try
                    {
                        await using var unlock = CriarComando("SELECT pg_advisory_unlock(@chave) WHERE pg_backend_pid() = @sessao");
                        unlock.CommandTimeout = 3;
                        if (await unlock.ExecuteScalarAsync(cleanup.Token) is not true)
                            throw new InvalidOperationException("O unlock do advisory lock não foi confirmado.");
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Falha ao liberar o advisory lock da Fonte {FonteId}, sessão {Sessao}.", fonteId, sessao);
                        PerderPosse();
                    }
                }
                else
                    NpgsqlConnection.ClearPool(connection);
            }
            finally
            {
                try
                {
                    await dbContext.Database.CloseConnectionAsync();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception,
                        "Falha ao devolver a conexão da Fonte {FonteId}, sessão {Sessao}, ao pool.",
                        fonteId, sessao);
                    NpgsqlConnection.ClearPool(connection);
                }
                finally
                {
                    onDispose();
                }
            }
        }
    }
}
