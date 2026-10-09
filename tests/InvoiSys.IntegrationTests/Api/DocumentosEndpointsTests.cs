using System.Net;
using FluentAssertions;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Documentos;
using InvoiSys.Domain.Enums;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class DocumentosEndpointsTests(ApiFixture api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_DeveRetornarDocumentosColetados_QuandoBuscarPeloTitulo()
    {
        var (token, _) = await ColetarFeedAsync();

        var pagina = await ListarAsync($"Busca={token}");

        pagina.Total.Should().Be(2);
        pagina.Itens.Should().HaveCount(2);
        pagina.Itens.Should().OnlyContain(documento =>
            documento.Titulo.Contains(token));
        pagina.Itens.Select(documento => documento.Titulo).Should().BeEquivalentTo(
            $"Nota fiscal {token} A",
            $"Nota fiscal {token} B");
    }

    [Fact]
    public async Task Listar_DeveRetornarVazio_QuandoBuscaNaoEncontrarNada()
    {
        var pagina = await ListarAsync($"Busca={ApiFixture.NovoToken()}");

        pagina.Total.Should().Be(0);
        pagina.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_DevePaginarResultados()
    {
        var (token, _) = await ColetarFeedAsync();

        var primeira = await ListarAsync($"Busca={token}&Pagina=1&TamanhoPagina=1");
        var segunda = await ListarAsync($"Busca={token}&Pagina=2&TamanhoPagina=1");

        primeira.Total.Should().Be(2);
        primeira.TotalPaginas.Should().Be(2);
        primeira.Itens.Should().HaveCount(1);
        segunda.Total.Should().Be(2);
        segunda.Itens.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Pagina=0")]
    [InlineData("TamanhoPagina=0")]
    [InlineData("TamanhoPagina=101")]
    public async Task Listar_DeveRetornar400_QuandoPaginacaoForInvalida(string query)
    {
        using var response = await api.Admin.GetAsync($"/api/documentos?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_DeveRetornar400_QuandoBuscaExcederOLimite()
    {
        var busca = new string('a', 201);

        using var response = await api.Admin.GetAsync(
            $"/api/documentos?Busca={busca}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Consultar_DeveRetornarDocumentoCompleto_QuandoExistir()
    {
        var (token, execucaoId) = await ColetarFeedAsync();
        var listados = await ListarAsync($"Busca={token}");
        var alvo = listados.Itens.Single(documento =>
            documento.Titulo == $"Nota fiscal {token} A");

        using var response = await api.Consulta.GetAsync(
            $"/api/documentos/{alvo.Id}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var documento = await response.LerDadosAsync<DocumentoResponse>(Ct);
        documento.Id.Should().Be(alvo.Id);
        documento.ExecucaoColetaId.Should().Be(execucaoId);
        documento.Titulo.Should().Be($"Nota fiscal {token} A");
        documento.UrlOriginal.Should().Contain($"/documentos/{token}/a");
        documento.Tipo.Should().Be("RSS");
        documento.Origem.Should().Be(api.Feed.RssUrl(token));
        documento.ConteudoTextual.Should().Contain($"Primeiro documento {token}");
        documento.DataPublicacao.Should().NotBeNull();
        documento.DataColeta.Should().BeCloseTo(
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5));
        documento.Hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Consultar_DeveRetornar404_QuandoDocumentoNaoExistir()
    {
        using var response = await api.Admin.GetAsync(
            $"/api/documentos/{Guid.NewGuid()}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("Documento não encontrado");
    }

    /// <summary>
    /// Cadastra uma Fonte RSS com conteúdo único e executa uma coleta,
    /// gerando dois documentos cujos títulos carregam o token devolvido.
    /// </summary>
    private async Task<(string Token, Guid ExecucaoId)> ColetarFeedAsync()
    {
        var token = ApiFixture.NovoToken();
        var fonte = await api.CriarFonteAsync(token, TipoFonte.Rss, Ct);
        var execucao = await api.ExecutarColetaAsync(fonte.Id, Ct);

        execucao.QuantidadeDocumentos.Should().Be(2);

        return (token, execucao.Id);
    }

    private async Task<PagedResponse<DocumentoResponse>> ListarAsync(string query)
    {
        using var response = await api.Admin.GetAsync($"/api/documentos?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.LerDadosAsync<PagedResponse<DocumentoResponse>>(Ct);
    }
}
