using System.Net;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace InvoiSys.IntegrationTests.Api;

public sealed class ApiSmokeTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    [Fact(Skip = "Requer Docker e a migration InitialCreate gerada antes da execução.")]
    public async Task Api_DeveResponderAlive_QuandoAmbienteEstiverDisponivel()
    {
        var cancellationToken = CancellationToken.None;
        var jwtKey = new string('x', 96);

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.InvoiSys_AppHost>(
            [
                "Parameters:postgres-password=InvoiSys_Test_123!",
                $"Parameters:jwt-key={jwtKey}",
                "Parameters:admin-email=admin@invoisys.test",
                "Parameters:admin-password=InvoiSys_Admin_123!"
            ],
            cancellationToken);

        await using var app = await builder.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var client = app.CreateHttpClient("api");
        using var response = await client.GetAsync("/alive", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
