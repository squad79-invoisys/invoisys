using System.Net;
using FluentAssertions;
using InvoiSys.Collectors.Anexos;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Collectors;

public sealed class AnexoDescobridorTests
{
    private const string UrlPagina =
        "https://www.gov.br/receita/pt-br/contratos/termo-no-01-2026.pdf/view";

    // Reproduz a estrutura da página do gov.br: o menu tem um link para o ".pdf"
    // e o arquivo de verdade está em "@@download/file", dentro de <main>.
    private const string PaginaGovBr = """
        <html>
          <head>
            <base href="https://www.gov.br/receita/pt-br/contratos/termo-no-01-2026.pdf/view" />
            <title>Termo</title>
          </head>
          <body>
            <nav>
              <a href="https://www.gov.br/receita/pt-br/contratos/termo-no-01-2026.pdf">GOV.BR</a>
              <a href="/receita/pt-br/manual.pdf">Manual do site</a>
            </nav>
            <main id="main-content">
              <h1>Termo de apostilamento</h1>
              <p>
                <a href="https://www.gov.br/receita/pt-br/contratos/termo-no-01-2026.pdf/@@download/file">
                  <img src="pdf.png" alt="application/pdf" />
                  Termo de Apostilamento nº 01 2026.PDF
                </a>
              </p>
            </main>
            <footer><a href="/receita/pt-br/carta.pdf">Carta de serviços</a></footer>
          </body>
        </html>
        """;

    [Fact]
    public async Task DescobrirAsync_DeveEncontrarSoOArquivoDoConteudoPrincipal()
    {
        var descobridor = Criar(_ => RespostasStub.Html(PaginaGovBr));

        var resultado = await descobridor.DescobrirAsync(UrlPagina, TestContext.Current.CancellationToken);

        resultado.Completa.Should().BeTrue();
        var anexo = resultado.Anexos.Should().ContainSingle().Subject;
        anexo.Url.Should().Be(
            "https://www.gov.br/receita/pt-br/contratos/termo-no-01-2026.pdf/@@download/file");
        anexo.Nome.Should().Be("Termo de Apostilamento nº 01 2026.PDF");
    }

    [Fact]
    public async Task DescobrirAsync_DeveResolverLinksRelativosPelaBaseDaPagina()
    {
        const string html = """
            <html><head><base href="https://exemplo.com/docs/" /></head>
            <body><main><a href="relatorio.pdf">Relatório</a></main></body></html>
            """;
        var descobridor = Criar(_ => RespostasStub.Html(html));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/noticia", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().ContainSingle()
            .Which.Url.Should().Be("https://exemplo.com/docs/relatorio.pdf");
    }

    [Fact]
    public async Task DescobrirAsync_DeveUsarCorpoQuandoNaoHaMainNemArticle()
    {
        const string html = """
            <html><body><p><a href="https://exemplo.com/a.xlsx">Planilha</a></p></body></html>
            """;
        var descobridor = Criar(_ => RespostasStub.Html(html));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/p", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().ContainSingle().Which.Nome.Should().Be("Planilha");
    }

    [Fact]
    public async Task DescobrirAsync_NaoDeveTratarPaginaDeVisualizacaoComoArquivo()
    {
        const string html = """
            <html><body><main>
              <a href="https://exemplo.com/a.pdf/view">Ver</a>
              <a href="mailto:alguem@exemplo.com">Contato</a>
              <a href="#topo">Topo</a>
              <a href="javascript:void(0)">Nada</a>
              <a href="/pagina.html">Outra página</a>
            </main></body></html>
            """;
        var descobridor = Criar(_ => RespostasStub.Html(html));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/p", TestContext.Current.CancellationToken);

        resultado.Completa.Should().BeTrue();
        resultado.Anexos.Should().BeEmpty();
    }

    [Fact]
    public async Task DescobrirAsync_DeveRemoverLinksRepetidosELimitarAQuantidade()
    {
        var links = string.Concat(Enumerable.Range(0, 40)
            .Select(i => $"<a href=\"/arq{i}.pdf\">Arq {i}</a><a href=\"/arq{i}.pdf\">Repetido</a>"));
        var descobridor = Criar(_ => RespostasStub.Html($"<html><body><main>{links}</main></body></html>"));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/p", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().HaveCount(AnexoDescobridor.LimiteAnexos);
        resultado.Anexos.Select(a => a.Url).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task DescobrirAsync_DeveTratarAPropriaUrlComoAnexoQuandoEhUmArquivo()
    {
        var descobridor = Criar(_ => RespostasStub.Arquivo([0x25, 0x50, 0x44, 0x46]));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/oficio", TestContext.Current.CancellationToken);

        resultado.Completa.Should().BeTrue();
        var anexo = resultado.Anexos.Should().ContainSingle().Subject;
        anexo.Url.Should().Be("https://exemplo.com/oficio");
        anexo.TipoConteudo.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DescobrirAsync_NaoDeveTratarFeedComoAnexo()
    {
        var descobridor = Criar(_ => RespostasStub.Arquivo([1], "application/rss+xml"));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/feed", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().BeEmpty();
        resultado.Completa.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DescobrirAsync_DeveMarcarComoIncompletaQuandoAPaginaRecusa(HttpStatusCode status)
    {
        var descobridor = Criar(_ => new HttpResponseMessage(status));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/p", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().BeEmpty();
        resultado.Completa.Should().BeFalse();
    }

    [Fact]
    public async Task DescobrirAsync_DeveMarcarComoIncompletaQuandoARedeFalha()
    {
        var descobridor = Criar(_ => throw new HttpRequestException("sem rede"));

        var resultado = await descobridor.DescobrirAsync(
            "https://exemplo.com/p", TestContext.Current.CancellationToken);

        resultado.Anexos.Should().BeEmpty();
        resultado.Completa.Should().BeFalse();
    }

    [Theory]
    [InlineData("ftp://exemplo.com/a.pdf")]
    [InlineData("file:///etc/passwd")]
    [InlineData("nao-e-url")]
    public async Task DescobrirAsync_NaoDeveConsultarEsquemasNaoHttp(string url)
    {
        var handler = new RespostaStubHandler(_ => RespostasStub.Html("<html></html>"));
        var descobridor = new AnexoDescobridor(
            new HttpClient(handler), NullLogger<AnexoDescobridor>.Instance);

        var resultado = await descobridor.DescobrirAsync(url, TestContext.Current.CancellationToken);

        resultado.Anexos.Should().BeEmpty();
        handler.Requisicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task DescobrirAsync_DevePropagarCancelamentoDoChamador()
    {
        using var cancelamento = new CancellationTokenSource();
        await cancelamento.CancelAsync();
        var descobridor = Criar(_ => RespostasStub.Html("<html></html>"));

        var act = () => descobridor.DescobrirAsync("https://exemplo.com/p", cancelamento.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static AnexoDescobridor Criar(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new RespostaStubHandler(responder)), NullLogger<AnexoDescobridor>.Instance);
}
