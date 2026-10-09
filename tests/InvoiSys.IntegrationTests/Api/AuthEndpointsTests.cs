using System.Net;
using FluentAssertions;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(ApiFixture api)
{
    private const string CookieRefresh = "invoisys.refresh_token";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static LoginRequest CredenciaisAdmin =>
        new(ApiFixture.AdminEmail, ApiFixture.AdminSenha);

    [Fact]
    public async Task Login_DeveRetornarTokenEPerfis_QuandoCredenciaisValidas()
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/login",
            CredenciaisAdmin,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await response.LerDadosAsync<LoginResponse>(Ct);
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.AccessTokenExpiraEm.Should().BeAfter(DateTimeOffset.UtcNow);
        login.UsuarioId.Should().Be(api.AdminId);
        login.Email.Should().Be(ApiFixture.AdminEmail);
        login.Perfis.Should().Contain("Administrador");
    }

    [Fact]
    public async Task Login_DeveDefinirCookieHttpOnly_QuandoCredenciaisValidas()
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/login",
            CredenciaisAdmin,
            Ct);

        var setCookie = response.ObterSetCookie(CookieRefresh);

        setCookie.Should().NotBeNull();
        setCookie!.ToLowerInvariant().Should().Contain("httponly");
    }

    [Theory]
    [InlineData(ApiFixture.AdminEmail, "SenhaErrada_123!")]
    [InlineData("naoexiste@invoisys.test", ApiFixture.AdminSenha)]
    public async Task Login_DeveRetornar401_QuandoCredenciaisInvalidas(
        string email,
        string senha)
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/login",
            new LoginRequest(email, senha),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Sucesso.Should().BeFalse();
        corpo.Mensagem.Should().Contain("E-mail ou senha inválidos");
        response.ObterSetCookie(CookieRefresh).Should().BeNull();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("isto-nao-e-um-email", "qualquer")]
    [InlineData(ApiFixture.AdminEmail, "")]
    public async Task Login_DeveRetornar400_QuandoPayloadInvalido(
        string email,
        string senha)
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/login",
            new LoginRequest(email, senha),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_DeveEmitirNovoTokenERotacionarCookie_QuandoCookieValido()
    {
        var (_, cookie) = await AutenticarAsync(
            ApiFixture.AdminEmail,
            ApiFixture.AdminSenha);

        using var response = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct,
            cookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await response.LerDadosAsync<LoginResponse>(Ct);
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Perfis.Should().Contain("Administrador");

        var novoCookie = response.ObterSetCookie(CookieRefresh);
        novoCookie.Should().NotBeNull();
        novoCookie!.ParaHeaderCookie().Should().NotBe(cookie);
    }

    [Fact]
    public async Task Refresh_DeveRetornar401_QuandoTokenAntigoForReutilizado()
    {
        var (_, cookie) = await AutenticarAsync(
            ApiFixture.AdminEmail,
            ApiFixture.AdminSenha);

        using var primeira = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct,
            cookie);
        primeira.StatusCode.Should().Be(HttpStatusCode.OK);

        using var segunda = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct,
            cookie);

        segunda.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_DeveRetornar401_QuandoNaoHouverCookie()
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_DeveRetornar401_QuandoCookieForInvalido()
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct,
            $"{CookieRefresh}=token-que-nao-existe");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_DeveRevogarSessao_QuandoCookieValido()
    {
        var (login, cookie) = await AutenticarAsync(
            ApiFixture.AdminEmail,
            ApiFixture.AdminSenha);

        var sessao = api.CriarClienteAutenticado(login.AccessToken);

        using (var antes = await sessao.GetAsync("/api/dashboard/resumo", Ct))
            antes.StatusCode.Should().Be(HttpStatusCode.OK);

        using var logout = await api.Anonimo.PostAsync(
            "/api/auth/logout",
            null,
            Ct,
            cookie);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);
        (await logout.LerApiAsync<object>(Ct)).Sucesso.Should().BeTrue();

        using var refresh = await api.Anonimo.PostAsync(
            "/api/auth/refresh",
            null,
            Ct,
            cookie);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var depois = await sessao.GetAsync("/api/dashboard/resumo", Ct);
        depois.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_DeveRetornar200_QuandoNaoHouverCookie()
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/logout",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.LerApiAsync<object>(Ct)).Sucesso.Should().BeTrue();
    }

    private async Task<(LoginResponse Login, string Cookie)> AutenticarAsync(
        string email,
        string senha)
    {
        using var response = await api.Anonimo.PostAsync(
            "/api/auth/login",
            new LoginRequest(email, senha),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await response.LerDadosAsync<LoginResponse>(Ct);
        var setCookie = response.ObterSetCookie(CookieRefresh);
        setCookie.Should().NotBeNull();

        return (login, setCookie!.ParaHeaderCookie());
    }
}
