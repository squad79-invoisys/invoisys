using Aspire.Hosting;
using Aspire.Hosting.Testing;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private DistributedApplication? _app;
    private string? _connectionString;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("O PostgreSQL de teste ainda não foi inicializado.");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.InvoiSys_AppHost>(
            [
                "Parameters:postgres-password=InvoiSys_Test_123!",
                $"Parameters:jwt-key={new string('x', 96)}",
                "Parameters:admin-email=admin@invoisys.test",
                "Parameters:admin-password=InvoiSys_Admin_123!",
                "UsePersistentDatabase=false"
            ],
            cancellationToken);

        _app = await builder.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await _app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        _connectionString = await _app.GetConnectionStringAsync(
            "invoisys",
            cancellationToken);

        await AguardarMigrationAsync(cancellationToken);
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private async Task AguardarMigrationAsync(CancellationToken cancellationToken)
    {
        var limite = DateTimeOffset.UtcNow.Add(DefaultTimeout);

        while (DateTimeOffset.UtcNow < limite)
        {
            try
            {
                await using var dbContext = CreateDbContext();
                var migrations = await dbContext.Database
                    .GetAppliedMigrationsAsync(cancellationToken);

                if (migrations.Contains("20261013221535_AdicionaIndiceUnicoDocumentoUrlHash"))
                    return;
            }
            catch (Npgsql.NpgsqlException)
            {
                // O banco e a API podem ainda estar iniciando.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException("As migrations do PostgreSQL de teste não foram aplicadas a tempo.");
    }
}
