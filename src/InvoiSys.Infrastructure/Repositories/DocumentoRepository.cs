using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace InvoiSys.Infrastructure.Repositories;

public sealed class DocumentoRepository(
    ApplicationDbContext dbContext) : IDocumentoRepository
{
    private const int TamanhoLote = 250;

    private const string InsertSql = """
        INSERT INTO documentos (
            "Id", "ExecucaoColetaId", "Titulo", "UrlOriginal", "Tipo", "Origem",
            "Contexto", "DataPublicacao", "DataColeta", "ConteudoTextual", "Metadados", "Hash")
        VALUES (
            $1, $2, $3, $4, $5, $6,
            $7, $8, $9, $10, $11, $12)
        ON CONFLICT ("UrlOriginal", "Hash") DO NOTHING;
        """;

    public async Task<int> InserirNovosAsync(
        IReadOnlyCollection<Documento> documentos,
        CancellationToken cancellationToken)
    {
        if (documentos.Count == 0)
            return 0;

        var transacaoAtual = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "A inserção de documentos exige uma transação em andamento.");

        var documentosUnicos = documentos
            .DistinctBy(documento => (documento.UrlOriginal, documento.Hash))
            .ToList();

        var urls = documentosUnicos
            .Select(documento => documento.UrlOriginal)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var paresExistentes = await dbContext.Documentos
            .AsNoTracking()
            .Where(documento => urls.Contains(documento.UrlOriginal))
            .Select(documento => new { documento.UrlOriginal, documento.Hash })
            .ToListAsync(cancellationToken);

        var chavesExistentes = paresExistentes
            .Select(documento => (documento.UrlOriginal, documento.Hash))
            .ToHashSet();

        var candidatos = documentosUnicos
            .Where(documento => !chavesExistentes.Contains(
                (documento.UrlOriginal, documento.Hash)))
            .ToList();

        if (candidatos.Count == 0)
            return 0;

        var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException(
                "A conexão configurada não é uma conexão PostgreSQL do Npgsql.");

        var transaction = transacaoAtual.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException(
                "A transação configurada não é uma transação PostgreSQL do Npgsql.");

        var totalInserido = 0;

        foreach (var lote in candidatos.Chunk(TamanhoLote))
        {
            await using var batch = new NpgsqlBatch(connection, transaction);

            foreach (var documento in lote)
                batch.BatchCommands.Add(CriarComando(documento));

            totalInserido += await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        return totalInserido;
    }

    public Task<Documento?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                documento => documento.Id == id,
                cancellationToken);

    public async Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        string? contexto,
        string? busca,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Documentos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(contexto))
        {
            var contextoNormalizado = contexto.Trim();
            query = query.Where(documento => documento.Contexto == contextoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(documento =>
                EF.Functions.ILike(documento.Titulo, $"%{busca}%") ||
                EF.Functions.ILike(documento.Origem, $"%{busca}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(documento => documento.DataColeta)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    private static NpgsqlBatchCommand CriarComando(Documento documento)
    {
        var command = new NpgsqlBatchCommand(InsertSql);

        command.Parameters.Add(CriarParametro(NpgsqlDbType.Uuid, documento.Id));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Uuid, documento.ExecucaoColetaId));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.Titulo));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.UrlOriginal));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.Tipo));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.Origem));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.Contexto));
        command.Parameters.Add(CriarParametro(
            NpgsqlDbType.TimestampTz,
            documento.DataPublicacao));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.TimestampTz, documento.DataColeta));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Text, documento.ConteudoTextual));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Jsonb, documento.Metadados));
        command.Parameters.Add(CriarParametro(NpgsqlDbType.Varchar, documento.Hash));

        return command;
    }

    private static NpgsqlParameter CriarParametro(
        NpgsqlDbType tipo,
        object? valor) =>
        new()
        {
            NpgsqlDbType = tipo,
            Value = valor ?? DBNull.Value
        };
}
