using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Coletas;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Dashboard.ObterResumo;
using InvoiSys.Application.Fontes;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Application.Usuarios.CriarUsuario;
using InvoiSys.Domain.Enums;

namespace InvoiSys.IntegrationTests.Support;

/// <summary>
/// Sobe o AppHost (API + PostgreSQL) uma única vez para todos os testes de
/// endpoint, junto com um servidor de feeds local e um usuário de cada perfil.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin@invoisys.test";
    public const string AdminSenha = "InvoiSys_Admin_123!";

    /// <summary>Senha que atende a política de senhas do cadastro de usuários.</summary>
    public const string SenhaPadrao = "InvoiSys_User_123!";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;
    private FakeFeedServer? _feed;
    private HttpClient? _http;

    public HttpClient Http =>
        _http ?? throw new InvalidOperationException("O ambiente não foi iniciado.");

    public FakeFeedServer Feed =>
        _feed ?? throw new InvalidOperationException("O ambiente não foi iniciado.");

    public ApiTestClient Anonimo { get; private set; } = null!;
    public ApiTestClient Admin { get; private set; } = null!;
    public ApiTestClient Operador { get; private set; } = null!;
    public ApiTestClient Consulta { get; private set; } = null!;

    public Guid AdminId { get; private set; }
    public Guid OperadorId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);
        var cancellationToken = cts.Token;

        _feed = await FakeFeedServer.StartAsync(cancellationToken);

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

        _app = await builder.BuildAsync(cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        await _app.StartAsync(cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        // O cliente do Aspire serve só para descobrir o endereço da API. Os testes
        // usam um HttpClient próprio, sem CookieContainer, para controlar os cookies.
        using (var descoberta = _app.CreateHttpClient("api"))
        {
            var baseAddress = descoberta.BaseAddress ?? throw new InvalidOperationException(
                "Não foi possível descobrir o endereço da API.");

            _http = new HttpClient(new HttpClientHandler { UseCookies = false })
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromMinutes(2)
            };
        }

        Anonimo = new ApiTestClient(Http, null);

        var admin = await LoginAsync(AdminEmail, AdminSenha, cancellationToken);
        AdminId = admin.UsuarioId;
        Admin = CriarClienteAutenticado(admin.AccessToken);

        var operador = await CriarUsuarioAsync("Operador", cancellationToken);
        var sessaoOperador = await LoginAsync(operador.Email, SenhaPadrao, cancellationToken);
        OperadorId = sessaoOperador.UsuarioId;
        Operador = CriarClienteAutenticado(sessaoOperador.AccessToken);

        var consulta = await CriarUsuarioAsync("Consulta", cancellationToken);
        var sessaoConsulta = await LoginAsync(consulta.Email, SenhaPadrao, cancellationToken);
        Consulta = CriarClienteAutenticado(sessaoConsulta.AccessToken);
    }

    public async ValueTask DisposeAsync()
    {
        _http?.Dispose();

        if (_app is not null)
            await _app.DisposeAsync();

        if (_feed is not null)
            await _feed.DisposeAsync();
    }

    public ApiTestClient CriarClienteAutenticado(string accessToken) =>
        new(Http, accessToken);

    public ApiTestClient ClientePorPerfil(string perfil) => perfil switch
    {
        "Administrador" => Admin,
        "Operador" => Operador,
        "Consulta" => Consulta,
        _ => throw new ArgumentOutOfRangeException(
            nameof(perfil),
            perfil,
            "Perfil desconhecido.")
    };

    public static string NovoToken() => Guid.NewGuid().ToString("N");

    public static string EmailUnico(string prefixo) =>
        $"{prefixo.ToLowerInvariant()}.{NovoToken()}@invoisys.test";

    public async Task<LoginResponse> LoginAsync(
        string email,
        string senha,
        CancellationToken cancellationToken)
    {
        using var response = await Anonimo.PostAsync(
            "/api/auth/login",
            new LoginRequest(email, senha),
            cancellationToken);

        await response.GarantirSucessoAsync(cancellationToken);

        return await response.LerDadosAsync<LoginResponse>(cancellationToken);
    }

    public async Task<UserSummary> CriarUsuarioAsync(
        string perfil,
        CancellationToken cancellationToken)
    {
        var request = new CriarUsuarioRequest(
            $"Usuário {perfil}",
            EmailUnico(perfil),
            SenhaPadrao,
            perfil);

        using var response = await Admin.PostAsync(
            "/api/usuarios",
            request,
            cancellationToken);

        await response.GarantirSucessoAsync(cancellationToken);

        return await response.LerDadosAsync<UserSummary>(cancellationToken);
    }

    public Task<FonteResponse> CriarFonteAsync(
        string token,
        TipoFonte tipo,
        CancellationToken cancellationToken)
    {
        var url = tipo == TipoFonte.Atom
            ? Feed.AtomUrl(token)
            : Feed.RssUrl(token);

        return CriarFonteComUrlAsync($"Fonte {token}", url, tipo, cancellationToken);
    }

    public async Task<FonteResponse> CriarFonteComUrlAsync(
        string nome,
        string url,
        TipoFonte tipo,
        CancellationToken cancellationToken)
    {
        using var response = await Admin.PostAsync(
            "/api/fontes",
            new CriarFonteRequest(nome, url, tipo, 60),
            cancellationToken);

        await response.GarantirSucessoAsync(cancellationToken);

        return await response.LerDadosAsync<FonteResponse>(cancellationToken);
    }

    public async Task DesativarFonteAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        using var response = await Admin.PatchAsync(
            $"/api/fontes/{fonteId}/desativar",
            null,
            cancellationToken);

        await response.GarantirSucessoAsync(cancellationToken);
    }

    public async Task<ExecucaoColetaResponse> ExecutarColetaAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        using var response = await Admin.PostAsync(
            $"/api/coletas/fontes/{fonteId}/executar",
            null,
            cancellationToken);

        await response.GarantirSucessoAsync(cancellationToken);

        return await response.LerDadosAsync<ExecucaoColetaResponse>(cancellationToken);
    }

    public async Task<ResumoDashboardResponse> ObterResumoAsync(
        CancellationToken cancellationToken)
    {
        using var response = await Admin.GetAsync(
            "/api/dashboard/resumo",
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.LerDadosAsync<ResumoDashboardResponse>(cancellationToken);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
