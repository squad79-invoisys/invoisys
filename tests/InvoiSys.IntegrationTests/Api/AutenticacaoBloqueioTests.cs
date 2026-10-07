using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace InvoiSys.IntegrationTests.Api;

public sealed class AutenticacaoBloqueioTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private const string AdminEmail = "admin@invoisys.test";
    private const string AdminSenha = "InvoiSys_Admin_123!";

    [Fact]
    public async Task Login_DeveBloquearConta_QuandoHouverCincoTentativasComSenhaErrada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var jwtKey = new string('x', 96);

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.InvoiSys_AppHost>(
            [
                "Parameters:postgres-password=InvoiSys_Test_123!",
                $"Parameters:jwt-key={jwtKey}",
                $"Parameters:admin-email={AdminEmail}",
                $"Parameters:admin-password={AdminSenha}",
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

        using var client = app.CreateHttpClient("api");

        for (var tentativa = 1; tentativa <= 5; tentativa++)
        {
            using var falha = await LoginAsync(
                client,
                "Senha_Errada_123!",
                cancellationToken);

            falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using var response = await LoginAsync(
            client,
            AdminSenha,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync(cancellationToken))
            .Should().Contain("Conta bloqueada temporariamente");
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string senha,
        CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = AdminEmail, Senha = senha },
            cancellationToken);
}
