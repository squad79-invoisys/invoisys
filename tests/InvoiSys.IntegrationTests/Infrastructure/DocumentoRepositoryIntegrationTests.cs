using System.Text;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace InvoiSys.IntegrationTests.Infrastructure;

[Collection("Aspire")]
public sealed class DocumentoRepositoryIntegrationTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task InserirNovosAsync_DevePersistirCamposEIgnorarDuplicata()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (execucao, _) = await CriarExecucaoAsync(dbContext);
        var repository = new DocumentoRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var dataPublicacao = DateTimeOffset.UtcNow.AddDays(-1);
        var documento = new Documento(
            execucao.Id,
            "Título",
            UrlUnica(),
            "WEB",
            "Portal",
            "Legislação",
            dataPublicacao,
            "Conteúdo",
            "{\"origem\":\"teste\"}",
            Hash('A'));

        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var primeira = await repository.InserirNovosAsync(
            [documento],
            TestContext.Current.CancellationToken);
        var segunda = await repository.InserirNovosAsync(
            [documento],
            TestContext.Current.CancellationToken);
        await unitOfWork.CommitTransactionAsync(TestContext.Current.CancellationToken);

        primeira.Should().Be(1);
        segunda.Should().Be(0);

        var persistido = await dbContext.Documentos
            .AsNoTracking()
            .SingleAsync(x => x.Id == documento.Id, TestContext.Current.CancellationToken);
        persistido.Titulo.Should().Be("Título");
        persistido.Contexto.Should().Be("Legislação");
        persistido.DataPublicacao.Should().BeCloseTo(dataPublicacao, TimeSpan.FromMilliseconds(1));
        persistido.ConteudoTextual.Should().Be("Conteúdo");
        persistido.Metadados.Should().Be("{\"origem\": \"teste\"}");
    }

    [Fact]
    public async Task InserirNovosAsync_MesmaUrlComHashesDiferentes_DevePreservarVersoes()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (execucao, _) = await CriarExecucaoAsync(dbContext);
        var repository = new DocumentoRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var url = UrlUnica();
        var versoes = new[]
        {
            CriarDocumento(execucao.Id, url, Hash('A')),
            CriarDocumento(execucao.Id, url, Hash('B'))
        };

        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var inseridos = await repository.InserirNovosAsync(
            versoes,
            TestContext.Current.CancellationToken);
        await unitOfWork.CommitTransactionAsync(TestContext.Current.CancellationToken);

        inseridos.Should().Be(2);
        var persistidos = await dbContext.Documentos
            .AsNoTracking()
            .Where(x => x.UrlOriginal == url)
            .ToListAsync(TestContext.Current.CancellationToken);
        persistidos.Should().HaveCount(2);
    }

    [Fact]
    public async Task InserirNovosAsync_Concorrente_DeveRetornarUmEZero()
    {
        Guid primeiraExecucaoId;
        Guid segundaExecucaoId;

        await using (var setup = fixture.CreateDbContext())
        {
            var (primeira, fonte) = await CriarExecucaoAsync(setup);
            var segunda = ExecucaoColeta.IniciarManual(fonte.Id, Guid.NewGuid());
            setup.ExecucoesColeta.Add(segunda);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
            primeiraExecucaoId = primeira.Id;
            segundaExecucaoId = segunda.Id;
        }

        var url = UrlUnica();
        var hash = Hash('C');

        async Task<int> InserirAsync(Guid execucaoId)
        {
            await using var dbContext = fixture.CreateDbContext();
            var repository = new DocumentoRepository(dbContext);
            var unitOfWork = new UnitOfWork(dbContext);
            await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
            var quantidade = await repository.InserirNovosAsync(
                [CriarDocumento(execucaoId, url, hash)],
                TestContext.Current.CancellationToken);
            await unitOfWork.CommitTransactionAsync(TestContext.Current.CancellationToken);
            return quantidade;
        }

        var resultados = await Task.WhenAll(
            InserirAsync(primeiraExecucaoId),
            InserirAsync(segundaExecucaoId));

        resultados.Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public async Task IndiceUnico_DeveSuportarUrlAsciiCom2048Caracteres()
    {
        await ValidarUrlLongaAsync(CriarUrlLonga('a', 2048));
    }

    [Fact(Skip = "PostgreSQL 18.3 rejeita esta chave B-tree com SQLSTATE 54000; consulte docs/validacao-indice-documentos.md.")]
    public async Task IndiceUnico_DeveSuportarUrlMultibyteProximaDe2048Caracteres()
    {
        await ValidarUrlLongaAsync(CriarUrlLongaMultibyte(2000));
    }

    [Fact]
    public async Task IndiceUnico_DeveExistirERejeitarInsercaoDiretaDuplicada()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (execucao, _) = await CriarExecucaoAsync(dbContext);
        var url = UrlUnica();
        var hash = Hash('U');
        dbContext.Documentos.AddRange(
            CriarDocumento(execucao.Id, url, hash),
            CriarDocumento(execucao.Id, url, hash));

        var act = () => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        var postgresException = exception.Which.InnerException
            .Should().BeOfType<PostgresException>().Subject;
        postgresException.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.Should().Be("IX_documentos_UrlOriginal_Hash");
    }

    [Fact]
    public async Task Migration_DeveInterromperSemExcluirDuplicatasHistoricas()
    {
        var databaseName = $"invoisys_guard_{Guid.NewGuid():N}";
        var adminConnectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres"
        }.ConnectionString;
        var testConnectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = databaseName
        }.ConnectionString;

        await ExecutarNoAdminAsync($"CREATE DATABASE \"{databaseName}\";");

        try
        {
            var options = new DbContextOptionsBuilder<InvoiSys.Infrastructure.Data.ApplicationDbContext>()
                .UseNpgsql(testConnectionString)
                .Options;
            await using var dbContext = new InvoiSys.Infrastructure.Data.ApplicationDbContext(options);
            var migrator = dbContext.GetService<IMigrator>();
            await migrator.MigrateAsync(
                "20261005160439_AdicionaContextoFonteEDocumento",
                TestContext.Current.CancellationToken);

            var (execucao, _) = await CriarExecucaoAsync(dbContext);
            var url = UrlUnica();
            var hash = Hash('D');
            dbContext.Documentos.AddRange(
                CriarDocumento(execucao.Id, url, hash),
                CriarDocumento(execucao.Id, url, hash));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            dbContext.ChangeTracker.Clear();

            var act = () => migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

            var exception = await act.Should().ThrowAsync<PostgresException>();
            exception.Which.MessageText.Should().Contain("existem documentos duplicados");
            (await dbContext.Documentos.CountAsync(TestContext.Current.CancellationToken))
                .Should().Be(2);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecutarNoAdminAsync($"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{databaseName}' AND pid <> pg_backend_pid();
                """);
            await ExecutarNoAdminAsync($"DROP DATABASE IF EXISTS \"{databaseName}\";");
        }

        async Task ExecutarNoAdminAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }

    private async Task ValidarUrlLongaAsync(string url)
    {
        await using var dbContext = fixture.CreateDbContext();
        var (execucao, _) = await CriarExecucaoAsync(dbContext);
        var repository = new DocumentoRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var documento = CriarDocumento(execucao.Id, url, Hash('L'));

        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var quantidade = await repository.InserirNovosAsync(
            [documento],
            TestContext.Current.CancellationToken);
        await unitOfWork.CommitTransactionAsync(TestContext.Current.CancellationToken);

        quantidade.Should().Be(1,
            $"a URL possui {url.Length} caracteres e {Encoding.UTF8.GetByteCount(url)} bytes");
    }

    private static async Task<(ExecucaoColeta Execucao, Fonte Fonte)> CriarExecucaoAsync(
        DbContext dbContext)
    {
        var fonte = new Fonte(
            $"Fonte {Guid.NewGuid():N}",
            UrlUnica(),
            TipoFonte.WebHtml,
            30,
            Guid.NewGuid(),
            "Legislação");
        var execucao = ExecucaoColeta.IniciarManual(fonte.Id, Guid.NewGuid());
        dbContext.Add(fonte);
        dbContext.Add(execucao);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (execucao, fonte);
    }

    private static Documento CriarDocumento(Guid execucaoId, string url, string hash) =>
        new(
            execucaoId,
            "Documento",
            url,
            "WEB",
            "Portal",
            "Legislação",
            null,
            "Conteúdo",
            null,
            hash);

    private static string CriarUrlLonga(char caractere, int tamanho)
    {
        const string prefixo = "https://exemplo.com/";
        return prefixo + new string(caractere, tamanho - prefixo.Length);
    }

    private static string CriarUrlLongaMultibyte(int tamanho)
    {
        const string prefixo = "https://exemplo.com/";
        var caracteres = Enumerable.Range(0, tamanho - prefixo.Length)
            .Select(indice => (char)(0x4E00 + indice % 2_000))
            .ToArray();
        return prefixo + new string(caracteres);
    }

    private static string UrlUnica() => $"https://exemplo.com/{Guid.NewGuid():N}";
    private static string Hash(char caractere) => new(caractere, 64);
}
