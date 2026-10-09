using System.Net;
using FluentAssertions;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Usuarios.AlterarStatusUsuario;
using InvoiSys.Application.Usuarios.CriarUsuario;
using InvoiSys.Application.Usuarios.RedefinirSenha;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class UsuariosEndpointsTests(ApiFixture api)
{
    private const string NovaSenha = "NovaSenha_456!";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    [InlineData("Consulta")]
    public async Task Criar_DeveRetornar201_QuandoDadosValidos(string perfil)
    {
        var email = ApiFixture.EmailUnico(perfil);
        var request = new CriarUsuarioRequest(
            "Usuário de teste",
            email,
            ApiFixture.SenhaPadrao,
            perfil);

        using var response = await api.Admin.PostAsync("/api/usuarios", request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var usuario = await response.LerDadosAsync<UserSummary>(Ct);
        usuario.Id.Should().NotBeEmpty();
        usuario.Nome.Should().Be("Usuário de teste");
        usuario.Email.Should().Be(email);
        usuario.Ativo.Should().BeTrue();
        usuario.Perfis.Should().ContainSingle().Which.Should().Be(perfil);

        var login = await api.LoginAsync(email, ApiFixture.SenhaPadrao, Ct);
        login.Perfis.Should().Contain(perfil);
    }

    [Fact]
    public async Task Criar_DeveRetornar409_QuandoEmailJaExistir()
    {
        var existente = await api.CriarUsuarioAsync("Consulta", Ct);

        var request = new CriarUsuarioRequest(
            "Outro usuário",
            existente.Email,
            ApiFixture.SenhaPadrao,
            "Consulta");

        using var response = await api.Admin.PostAsync("/api/usuarios", request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Sucesso.Should().BeFalse();
        corpo.Mensagem.Should().Contain("Já existe um usuário com este e-mail");
    }

    [Theory]
    [InlineData("Curta1")]
    [InlineData("tudominusculo123")]
    [InlineData("TUDOMAIUSCULO123")]
    [InlineData("SemNumeroNenhum")]
    public async Task Criar_DeveRetornar400_QuandoSenhaForFraca(string senha)
    {
        var request = new CriarUsuarioRequest(
            "Usuário de teste",
            ApiFixture.EmailUnico("fraca"),
            senha,
            "Consulta");

        using var response = await api.Admin.PostAsync("/api/usuarios", request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.LerApiAsync<object>(Ct)).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoPerfilForInvalido()
    {
        var request = new CriarUsuarioRequest(
            "Usuário de teste",
            ApiFixture.EmailUnico("perfil"),
            ApiFixture.SenhaPadrao,
            "SuperUsuario");

        using var response = await api.Admin.PostAsync("/api/usuarios", request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("Perfil inválido");
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoEmailForInvalido()
    {
        var request = new CriarUsuarioRequest(
            "Usuário de teste",
            "isto-nao-e-email",
            ApiFixture.SenhaPadrao,
            "Consulta");

        using var response = await api.Admin.PostAsync("/api/usuarios", request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_DeveRetornarUsuarios_QuandoAdministrador()
    {
        var criado = await api.CriarUsuarioAsync("Operador", Ct);

        using var response = await api.Admin.GetAsync("/api/usuarios", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usuarios = await response.LerDadosAsync<List<UserSummary>>(Ct);
        usuarios.Should().Contain(usuario => usuario.Email == ApiFixture.AdminEmail);
        usuarios.Should().Contain(usuario =>
            usuario.Id == criado.Id &&
            usuario.Perfis.Contains("Operador"));
    }

    [Fact]
    public async Task AlterarStatus_DeveBloquearELiberarLogin_QuandoDesativarEReativar()
    {
        var usuario = await api.CriarUsuarioAsync("Consulta", Ct);

        using (var desativar = await api.Admin.PatchAsync(
                   $"/api/usuarios/{usuario.Id}/status",
                   new AlterarStatusUsuarioRequest(false),
                   Ct))
        {
            desativar.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var bloqueado = await api.Anonimo.PostAsync(
                   "/api/auth/login",
                   new LoginRequest(usuario.Email, ApiFixture.SenhaPadrao),
                   Ct))
        {
            bloqueado.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using (var reativar = await api.Admin.PatchAsync(
                   $"/api/usuarios/{usuario.Id}/status",
                   new AlterarStatusUsuarioRequest(true),
                   Ct))
        {
            reativar.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var login = await api.LoginAsync(usuario.Email, ApiFixture.SenhaPadrao, Ct);
        login.UsuarioId.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task AlterarStatus_DeveRevogarSessoesAtivas_QuandoDesativar()
    {
        var usuario = await api.CriarUsuarioAsync("Consulta", Ct);
        var login = await api.LoginAsync(usuario.Email, ApiFixture.SenhaPadrao, Ct);
        var sessao = api.CriarClienteAutenticado(login.AccessToken);

        using (var antes = await sessao.GetAsync("/api/dashboard/resumo", Ct))
            antes.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var desativar = await api.Admin.PatchAsync(
                   $"/api/usuarios/{usuario.Id}/status",
                   new AlterarStatusUsuarioRequest(false),
                   Ct))
        {
            desativar.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var depois = await sessao.GetAsync("/api/dashboard/resumo", Ct);
        depois.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AlterarStatus_DeveRetornar404_QuandoUsuarioNaoExistir()
    {
        using var response = await api.Admin.PatchAsync(
            $"/api/usuarios/{Guid.NewGuid()}/status",
            new AlterarStatusUsuarioRequest(false),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RedefinirSenha_DeveTrocarSenhaERevogarSessoes_QuandoSenhaValida()
    {
        var usuario = await api.CriarUsuarioAsync("Operador", Ct);
        var sessaoAntiga = await api.LoginAsync(
            usuario.Email,
            ApiFixture.SenhaPadrao,
            Ct);

        using (var response = await api.Admin.PutAsync(
                   $"/api/usuarios/{usuario.Id}/senha",
                   new RedefinirSenhaRequest(NovaSenha),
                   Ct))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.LerApiAsync<object>(Ct)).Sucesso.Should().BeTrue();
        }

        using (var senhaAntiga = await api.Anonimo.PostAsync(
                   "/api/auth/login",
                   new LoginRequest(usuario.Email, ApiFixture.SenhaPadrao),
                   Ct))
        {
            senhaAntiga.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var novaSessao = await api.LoginAsync(usuario.Email, NovaSenha, Ct);
        novaSessao.UsuarioId.Should().Be(usuario.Id);

        using var tokenAntigo = await api
            .CriarClienteAutenticado(sessaoAntiga.AccessToken)
            .GetAsync("/api/fontes", Ct);
        tokenAntigo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RedefinirSenha_DeveRetornar400_QuandoSenhaForFraca()
    {
        var usuario = await api.CriarUsuarioAsync("Consulta", Ct);

        using var response = await api.Admin.PutAsync(
            $"/api/usuarios/{usuario.Id}/senha",
            new RedefinirSenhaRequest("fraca"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var login = await api.LoginAsync(usuario.Email, ApiFixture.SenhaPadrao, Ct);
        login.UsuarioId.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task RedefinirSenha_DeveRetornar404_QuandoUsuarioNaoExistir()
    {
        using var response = await api.Admin.PutAsync(
            $"/api/usuarios/{Guid.NewGuid()}/senha",
            new RedefinirSenhaRequest(NovaSenha),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
