using System.Net;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace InvoiSys.IntegrationTests.Api;

public sealed class ApiSmokeTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task Api_DeveResponderAlive_QuandoAmbienteEstiverDisponivel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var jwtKey = new string('x', 96);

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.InvoiSys_AppHost>(
            [
                "Parameters:postgres-password=InvoiSys_Test_123!",
                $"Parameters:jwt-key={jwtKey}",
                "Parameters:admin-email=admin@invoisys.test",
                "Parameters:admin-password=InvoiSys_Admin_123!",
                "UsePersistentDatabase=false"
            ],
            cancellationToken);

        await using var app = await builder.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // O cliente do Aspire serve só para descobrir o endereço da API. A chamada
        // usa um HttpClient que aceita o certificado de desenvolvimento, que não é
        // confiável em máquinas limpas como o runner do GitHub Actions.
        using var descoberta = app.CreateHttpClient("api");
        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            BaseAddress = descoberta.BaseAddress
        };

        using var response = await client.GetAsync("/alive", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}