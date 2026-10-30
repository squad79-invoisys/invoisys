using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using InvoiSys.Api.Workers;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using InvoiSys.Infrastructure.Repositories;
using InvoiSys.IntegrationTests.Infrastructure;
using InvoiSys.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace InvoiSys.IntegrationTests.Api;

[Collection("Aspire")]
public sealed class ColetaAutomaticaWorkerIntegrationTests
{
    [Fact]
    public async Task WorkerHabilitado_DeveColetarFonteVencidaSemColetarInativa()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var token = timeout.Token;
        await using var feed = await FakeFeedServer.StartAsync(token);
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.InvoiSys_AppHost>(
            [
                "Parameters:postgres-password=InvoiSys_Test_123!",
                $"Parameters:jwt-key={new string('x', 96)}",
                "Parameters:admin-email=admin@invoisys.test",
                "Parameters:admin-password=InvoiSys_Admin_123!",
                "UsePersistentDatabase=false",
                "ColetaAutomatica:Enabled=true",
                "ColetaAutomatica:IntervaloVerificacaoSegundos=1"
            ], token);
        await using var app = await builder.BuildAsync(token);
        await app.StartAsync(token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", token);

        var connectionString = await app.GetConnectionStringAsync("invoisys", token);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString).Options;
        var migrationAplicada = false;
        var limiteMigration = DateTimeOffset.UtcNow.AddMinutes(2);
        while (DateTimeOffset.UtcNow < limiteMigration && !migrationAplicada)
        {
            try
            {
                await using var db = new ApplicationDbContext(options);
                var migrations = await db.Database.GetAppliedMigrationsAsync(token);
                migrationAplicada = migrations.Contains(
                    "20261019195813_AdicionaFonteColetaLease");
            }
            catch (Npgsql.NpgsqlException)
            {
                // A API e o banco ainda podem estar inicializando.
            }
            if (!migrationAplicada)
                await Task.Delay(250, token);
        }
        migrationAplicada.Should().BeTrue();

        var ativa = new Fonte("RSS automática", feed.RssUrl(Guid.NewGuid().ToString("N")),
            TipoFonte.Rss, 1, Guid.NewGuid(), "Outro");
        var inativa = new Fonte("RSS inativa", feed.RssUrl(Guid.NewGuid().ToString("N")),
            TipoFonte.Rss, 1, Guid.NewGuid(), "Outro");
        inativa.Desativar(Guid.NewGuid());

        await using (var db = new ApplicationDbContext(options))
        {
            db.Fontes.AddRange(ativa, inativa);
            await db.SaveChangesAsync(token);
            await db.Fontes.Where(x => x.Id == ativa.Id || x.Id == inativa.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    x => x.CriadoEm, DateTimeOffset.UtcNow.AddMinutes(-5)), token);
        }

        ExecucaoColeta? encontrada = null;
        var limite = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < limite && encontrada is null)
        {
            await Task.Delay(250, token);
            await using var db = new ApplicationDbContext(options);
            encontrada = await db.ExecucoesColeta.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FonteId == ativa.Id &&
                    x.Status == StatusExecucaoColeta.Concluida, token);
        }

        encontrada.Should().NotBeNull();
        encontrada!.TipoExecucao.Should().Be(TipoExecucao.Automatica);
        encontrada.SolicitadaPorUsuarioId.Should().BeNull();
        await using (var db = new ApplicationDbContext(options))
        {
            (await db.ExecucoesColeta.CountAsync(x => x.FonteId == inativa.Id, token))
                .Should().Be(0);
        }
    }
}

[Collection("Aspire")]
public sealed class ColetaAutomaticaWorkerShutdownIntegrationTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StopAsync_DeveAguardarColetaRealELiberarPosse()
    {
        var fontes = new[]
        {
            new Fonte($"Fonte {Guid.NewGuid():N}", $"https://exemplo.com/{Guid.NewGuid():N}",
                TipoFonte.WebHtml, 1, Guid.NewGuid(), "Outro"),
            new Fonte($"Fonte {Guid.NewGuid():N}", $"https://exemplo.com/{Guid.NewGuid():N}",
                TipoFonte.WebHtml, 1, Guid.NewGuid(), "Outro")
        };
        await using (var db = fixture.CreateDbContext())
        {
            db.Fontes.AddRange(fontes);
            await db.SaveChangesAsync(Ct);
            await db.Fontes.Where(x => x.Id == fontes[0].Id || x.Id == fontes[1].Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    x => x.CriadoEm, DateTimeOffset.UtcNow.AddMinutes(-5)), Ct);
        }

        var coletor = new ColetorBloqueavel();
        var descarte = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));
        builder.Services.AddScoped<IFonteRepository, FonteRepository>();
        builder.Services.AddScoped<IExecucaoColetaRepository, ExecucaoColetaRepository>();
        builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IFonteColetaLock, PostgresFonteColetaLock>();
        builder.Services.AddScoped<IColetaExecutor, ColetaExecutor>();
        builder.Services.AddScoped<ICollectorResolver>(_ => new ResolverDescartavel(coletor, descarte));
        builder.Services.Configure<ColetaAutomaticaOptions>(options =>
        {
            options.IntervaloVerificacaoSegundos = 1;
            options.ParalelismoMaximo = 1;
            options.TimeoutFonteSegundos = 30;
        });
        builder.Services.Configure<HostOptions>(options =>
            options.ShutdownTimeout = TimeSpan.FromSeconds(15));
        builder.Services.AddHostedService<ColetaAutomaticaWorker>();

        using var host = builder.Build();
        Task? encerramento = null;
        var iniciado = false;
        try
        {
            await host.StartAsync(Ct);
            iniciado = true;
            await coletor.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);
            var primeira = fontes.Single(x => x.Url == coletor.UrlIniciada);
            var segunda = fontes.Single(x => x.Id != primeira.Id);

            await using (var db = fixture.CreateDbContext())
            {
                var aberta = await db.ExecucoesColeta.AsNoTracking()
                    .SingleAsync(x => x.FonteId == primeira.Id, Ct);
                aberta.TipoExecucao.Should().Be(TipoExecucao.Automatica);
                aberta.Status.Should().Be(StatusExecucaoColeta.EmAndamento);
                (await db.ExecucoesColeta.CountAsync(x => x.FonteId == segunda.Id, Ct))
                    .Should().Be(0);
            }
            (await LeaseValidoAsync(primeira.Id)).Should().BeTrue();
            await using (var lockScope = host.Services.CreateAsyncScope())
            {
                var ocupado = await lockScope.ServiceProvider
                    .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(primeira.Id, Ct);
                ocupado.Should().BeNull();
            }

            encerramento = host.StopAsync(Ct);
            await coletor.Cancelou.WaitAsync(TimeSpan.FromSeconds(15), Ct);
            encerramento.IsCompleted.Should().BeFalse();
            coletor.Liberar();
            await encerramento.WaitAsync(TimeSpan.FromSeconds(15), Ct);
            await coletor.Terminou.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            await descarte.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);

            await using (var db = fixture.CreateDbContext())
            {
                var encerrada = await db.ExecucoesColeta.AsNoTracking()
                    .SingleAsync(x => x.FonteId == primeira.Id, Ct);
                encerrada.Status.Should().Be(StatusExecucaoColeta.Falhou);
                encerrada.MensagemErro.Should().Contain("encerramento");
                (await db.Documentos.CountAsync(x => x.ExecucaoColetaId == encerrada.Id, Ct))
                    .Should().Be(0);
                (await db.ExecucoesColeta.CountAsync(x => x.FonteId == segunda.Id, Ct))
                    .Should().Be(0);
            }
            coletor.Acessos.Should().Be(1);
            (await LeaseValidoAsync(primeira.Id)).Should().BeFalse();
            await using var outroScope = host.Services.CreateAsyncScope();
            await using var liberado = await outroScope.ServiceProvider
                .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(primeira.Id, Ct);
            liberado.Should().NotBeNull();
        }
        finally
        {
            coletor.Liberar();
            if (encerramento is not null)
                await encerramento.WaitAsync(TimeSpan.FromSeconds(15), Ct);
            else if (iniciado)
                await host.StopAsync(Ct).WaitAsync(TimeSpan.FromSeconds(15), Ct);
        }
    }

    private async Task<bool> LeaseValidoAsync(Guid fonteId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("""
            SELECT valido_ate > clock_timestamp()
            FROM fontes_coleta_lease WHERE fonte_id = @fonte
            """, connection);
        command.Parameters.AddWithValue("fonte", fonteId);
        return (bool)(await command.ExecuteScalarAsync(Ct))!;
    }

    private sealed class ResolverDescartavel(
        ColetorBloqueavel coletor, TaskCompletionSource descarte)
        : ICollectorResolver, IAsyncDisposable
    {
        public IContentCollector Resolver(TipoFonte tipo) => coletor;
        public ValueTask DisposeAsync()
        {
            descarte.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ColetorBloqueavel : IContentCollector
    {
        private readonly TaskCompletionSource _entrou =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancelou =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _terminou =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _liberado =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _acessos;
        public string? UrlIniciada { get; private set; }
        public int Acessos => Volatile.Read(ref _acessos);
        public Task Entrou => _entrou.Task;
        public Task Cancelou => _cancelou.Task;
        public Task Terminou => _terminou.Task;
        public bool Suporta(TipoFonte tipo) => true;
        public void Liberar() => _liberado.TrySetResult();

        public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
            string url, string? seletorConteudo, CancellationToken cancellationToken)
        {
            UrlIniciada = url;
            Interlocked.Increment(ref _acessos);
            _entrou.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _cancelou.TrySetResult();
                await _liberado.Task;
                throw;
            }
            finally
            {
                _terminou.TrySetResult();
            }
            return [];
        }
    }
}
