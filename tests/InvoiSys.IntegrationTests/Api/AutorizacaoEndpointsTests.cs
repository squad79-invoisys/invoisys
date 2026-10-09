using System.Net;
using FluentAssertions;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

/// <summary>
/// Matriz de autorização dos endpoints protegidos: sem token (401), com perfil
/// insuficiente (403) e leitura liberada para qualquer usuário autenticado.
/// A autorização roda antes da ação, então o corpo e o id usados aqui não importam.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AutorizacaoEndpointsTests(ApiFixture api)
{
    private const string Id = "00000000-0000-0000-0000-000000000001";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/fontes")]
    [InlineData("GET", $"/api/fontes/{Id}")]
    [InlineData("POST", "/api/fontes")]
    [InlineData("PUT", $"/api/fontes/{Id}")]
    [InlineData("PATCH", $"/api/fontes/{Id}/ativar")]
    [InlineData("PATCH", $"/api/fontes/{Id}/desativar")]
    [InlineData("GET", "/api/coletas")]
    [InlineData("GET", $"/api/coletas/{Id}")]
    [InlineData("POST", $"/api/coletas/fontes/{Id}/executar")]
    [InlineData("GET", "/api/documentos")]
    [InlineData("GET", $"/api/documentos/{Id}")]
    [InlineData("GET", "/api/dashboard/resumo")]
    [InlineData("GET", "/api/usuarios")]
    [InlineData("POST", "/api/usuarios")]
    [InlineData("PATCH", $"/api/usuarios/{Id}/status")]
    [InlineData("PUT", $"/api/usuarios/{Id}/senha")]
    public async Task Endpoint_DeveRetornar401_QuandoNaoHouverToken(
        string metodo,
        string caminho)
    {
        using var response = await api.Anonimo.SendAsync(
            new HttpMethod(metodo),
            caminho,
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("GET", "/api/fontes")]
    [InlineData("GET", "/api/dashboard/resumo")]
    [InlineData("GET", "/api/usuarios")]
    public async Task Endpoint_DeveRetornar401_QuandoTokenForMalformado(
        string metodo,
        string caminho)
    {
        var cliente = api.CriarClienteAutenticado("isto.nao.e-um-jwt");

        using var response = await cliente.SendAsync(
            new HttpMethod(metodo),
            caminho,
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Consulta", "POST", "/api/fontes")]
    [InlineData("Consulta", "PUT", $"/api/fontes/{Id}")]
    [InlineData("Consulta", "PATCH", $"/api/fontes/{Id}/ativar")]
    [InlineData("Consulta", "PATCH", $"/api/fontes/{Id}/desativar")]
    [InlineData("Consulta", "POST", $"/api/coletas/fontes/{Id}/executar")]
    [InlineData("Consulta", "GET", "/api/usuarios")]
    [InlineData("Consulta", "POST", "/api/usuarios")]
    [InlineData("Consulta", "PATCH", $"/api/usuarios/{Id}/status")]
    [InlineData("Consulta", "PUT", $"/api/usuarios/{Id}/senha")]
    [InlineData("Operador", "GET", "/api/usuarios")]
    [InlineData("Operador", "POST", "/api/usuarios")]
    [InlineData("Operador", "PATCH", $"/api/usuarios/{Id}/status")]
    [InlineData("Operador", "PUT", $"/api/usuarios/{Id}/senha")]
    public async Task Endpoint_DeveRetornar403_QuandoPerfilNaoTemPermissao(
        string perfil,
        string metodo,
        string caminho)
    {
        using var response = await api.ClientePorPerfil(perfil).SendAsync(
            new HttpMethod(metodo),
            caminho,
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    [InlineData("Consulta")]
    public async Task Leitura_DeveRetornar200_QuandoUsuarioEstiverAutenticado(
        string perfil)
    {
        var cliente = api.ClientePorPerfil(perfil);

        foreach (var caminho in new[]
                 {
                     "/api/fontes",
                     "/api/coletas",
                     "/api/documentos",
                     "/api/dashboard/resumo"
                 })
        {
            using var response = await cliente.GetAsync(caminho, Ct);

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                $"o perfil {perfil} deve poder consultar {caminho}");
        }
    }
}
