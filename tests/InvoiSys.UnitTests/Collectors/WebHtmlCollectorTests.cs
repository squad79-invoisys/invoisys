using System.Net;
using System.Text;
using FluentAssertions;
using InvoiSys.Collectors.WebHtml;
using InvoiSys.Domain.Enums;

namespace InvoiSys.UnitTests.Collectors;

public sealed class WebHtmlCollectorTests
{
    private const string Url = "https://dfe-portal.svrs.rs.gov.br/";

    [Theory]
    [InlineData(TipoFonte.WebHtml, true)]
    [InlineData(TipoFonte.Rss, false)]
    [InlineData(TipoFonte.Atom, false)]
    public void Suporta_DeveAtenderApenasWebHtml(TipoFonte tipo, bool esperado) =>
        CriarCollector("<html><body>x</body></html>").Suporta(tipo).Should().Be(esperado);

    [Fact]
    public async Task ColetarAsync_DeveUsarOTitleDaPagina()
    {
        var collector = CriarCollector(
            "<html><head><title>Portal DF-e</title></head><body><h1>Outro</h1></body></html>");

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);

        documentos.Should().ContainSingle();
        documentos[0].Titulo.Should().Be("Portal DF-e");
    }

    [Fact]
    public async Task ColetarAsync_SemTitle_DeveUsarOPrimeiroH1()
    {
        var collector = CriarCollector(
            "<html><body><h1>Manual da NF-e</h1><p>Conteúdo</p></body></html>");

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);

        documentos[0].Titulo.Should().Be("Manual da NF-e");
    }

    [Fact]
    public async Task ColetarAsync_SemTituloAlgum_DeveUsarTextoPadrao()
    {
        var collector = CriarCollector("<html><body><p>Só texto</p></body></html>");

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);

        documentos[0].Titulo.Should().Be("Sem título");
    }

    [Fact]
    public async Task ColetarAsync_NaoDeveIncluirScriptStyleNavEFooter()
    {
        var collector = CriarCollector("""
            <html><head><title>Portal</title><style>.a{color:red}</style></head>
            <body>
              <nav>Menu lateral</nav>
              <main><p>Nota técnica publicada hoje.</p></main>
              <footer>Rodapé institucional</footer>
              <script>var x = 'script interno';</script>
            </body></html>
            """);

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);
        var conteudo = documentos[0].ConteudoTextual;

        conteudo.Should().Contain("Nota técnica publicada hoje.");
        conteudo.Should().NotContain("Menu lateral");
        conteudo.Should().NotContain("Rodapé institucional");
        conteudo.Should().NotContain("script interno");
        conteudo.Should().NotContain("color:red");
    }

    [Fact]
    public async Task ColetarAsync_DeveProduzirHashIdenticoParaPaginaInalterada()
    {
        const string html = "<html><head><title>Portal</title></head><body><p>Mesmo texto</p></body></html>";

        var primeira = await CriarCollector(html).ColetarAsync(Url, CancellationToken.None);
        var segunda = await CriarCollector(html).ColetarAsync(Url, CancellationToken.None);

        segunda[0].Hash.Should().Be(primeira[0].Hash);
        primeira[0].Hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ColetarAsync_ComConteudoAlterado_DeveProduzirHashDiferente()
    {
        var antes = await CriarCollector(
            "<html><head><title>Portal</title></head><body><p>Versão 1</p></body></html>")
            .ColetarAsync(Url, CancellationToken.None);

        var depois = await CriarCollector(
            "<html><head><title>Portal</title></head><body><p>Versão 2</p></body></html>")
            .ColetarAsync(Url, CancellationToken.None);

        depois[0].Hash.Should().NotBe(antes[0].Hash);
    }

    [Fact]
    public async Task ColetarAsync_DevePreferirArticleAoBody()
    {
        var collector = CriarCollector("""
            <html><head><title>Portal</title></head>
            <body>
              <p>Texto fora do artigo</p>
              <article><p>Texto do artigo</p></article>
            </body></html>
            """);

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);

        documentos[0].ConteudoTextual.Should().Be("Texto do artigo");
    }

    [Fact]
    public async Task ColetarAsync_DeveLerADataDePublicacaoDoMeta()
    {
        var collector = CriarCollector("""
            <html><head><title>Portal</title>
            <meta property="article:published_time" content="2026-09-10T12:00:00Z" />
            </head><body><p>Conteúdo</p></body></html>
            """);

        var documentos = await collector.ColetarAsync(Url, CancellationToken.None);

        documentos[0].DataPublicacao.Should().Be(
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task ColetarAsync_DevePreencherOrigemETipo()
    {
        var documentos = await CriarCollector(
            "<html><head><title>Portal</title></head><body><p>x</p></body></html>")
            .ColetarAsync(Url, CancellationToken.None);

        documentos[0].Tipo.Should().Be("WEB");
        documentos[0].Origem.Should().Be(Url);
        documentos[0].UrlOriginal.Should().Be(Url);
    }

    [Fact]
    public async Task ColetarAsync_ComRespostaDeErro_DeveLancarExcecao()
    {
        var collector = new WebHtmlCollector(
            new HttpClient(new RespostaFixaHandler("", HttpStatusCode.NotFound)));

        var acao = async () => await collector.ColetarAsync(Url, CancellationToken.None);

        await acao.Should().ThrowAsync<HttpRequestException>();
    }

    private static WebHtmlCollector CriarCollector(string html) =>
        new(new HttpClient(new RespostaFixaHandler(html, HttpStatusCode.OK)));

    private sealed class RespostaFixaHandler(
        string html,
        HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
    }
}
