using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace InvoiSys.IntegrationTests.Api;

public sealed class HistoricoTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private const string AdminEmail = "admin@invoisys.test";
    private const string AdminSenha = "InvoiSys_Admin_123!";

    [Fact]
    public async Task Historico_DeveListarEExportarEventosDeAutenticacao_QuandoAdministradorConsultar()
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

        using var falha = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = AdminEmail, Senha = "Senha_Errada_123!" },
            cancellationToken);
        falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = AdminEmail, Senha = AdminSenha },
            cancellationToken);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginJson = JsonDocument.Parse(
            await login.Content.ReadAsStringAsync(cancellationToken));
        var accessToken = loginJson.RootElement
            .GetProperty("dados")
            .GetProperty("accessToken")
            .GetString();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Datas com offset diferente de UTC: a API precisa normalizar antes de consultar o PostgreSQL.
        var de = Uri.EscapeDataString(DateTimeOffset.Now.AddHours(-1).ToOffset(TimeSpan.FromHours(-3)).ToString("o"));
        var ate = Uri.EscapeDataString(DateTimeOffset.Now.AddHours(1).ToOffset(TimeSpan.FromHours(-3)).ToString("o"));

        using var historico = await client.GetAsync(
            $"/api/historico?tipo=Autenticacao&de={de}&ate={ate}",
            cancellationToken);
        var corpo = await historico.Content.ReadAsStringAsync(cancellationToken);
        historico.StatusCode.Should().Be(HttpStatusCode.OK, corpo);

        using var historicoJson = JsonDocument.Parse(corpo);
        var dados = historicoJson.RootElement.GetProperty("dados");
        var eventos = dados.GetProperty("eventos").GetProperty("itens").EnumerateArray().ToList();

        eventos.Select(evento => evento.GetProperty("atividade").GetString())
            .Should().Equal("Login realizado", "Tentativa de login recusada");
        eventos.Should().OnlyContain(evento =>
            evento.GetProperty("responsavel").GetString() == AdminEmail &&
            !string.IsNullOrEmpty(evento.GetProperty("enderecoIp").GetString()));
        dados.GetProperty("totais").GetProperty("autenticacoes").GetInt32().Should().Be(2);

        using var busca = await client.GetAsync(
            "/api/historico?busca=recusada",
            cancellationToken);
        using var buscaJson = JsonDocument.Parse(
            await busca.Content.ReadAsStringAsync(cancellationToken));
        buscaJson.RootElement.GetProperty("dados").GetProperty("eventos")
            .GetProperty("total").GetInt32().Should().Be(1);

        using var exportacao = await client.GetAsync(
            "/api/historico/exportar?tipo=Autenticacao",
            cancellationToken);
        exportacao.StatusCode.Should().Be(HttpStatusCode.OK);
        exportacao.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csv = await exportacao.Content.ReadAsStringAsync(cancellationToken);
        csv.Should().Contain("Login realizado").And.Contain("Tentativa de login recusada");
    }
}
