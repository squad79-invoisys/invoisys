using System.Net;
using System.Text;
using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using InvoiSys.Collectors.RssAtom;

namespace InvoiSys.UnitTests.Collectors;

public sealed class RssAtomCollectorTests
{
    [Fact]
    public async Task ExtracaoRssGrande_DeveInterromperAoCancelar()
    {
        var itens = string.Concat(Enumerable.Range(0, 20000)
            .Select(i => $"<item><title>{i}</title><link>https://exemplo.com/{i}</link></item>"));
        var xml = XDocument.Parse($"<rss><channel>{itens}</channel></rss>");
        var metodo = typeof(RssAtomCollector).GetMethod("LerRss", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var cancellation = new CancellationTokenSource();
        using var iniciou = new ManualResetEventSlim();
        var extracao = Task.Run(() =>
        {
            iniciou.Set();
            try { metodo.Invoke(null, [xml, "https://exemplo.com/feed", cancellation.Token]); }
            catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException)
            { throw exception.InnerException; }
        }, TestContext.Current.CancellationToken);
        iniciou.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).Should().BeTrue();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(1));
        var act = () => extracao;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ColetarAsync_DevePreservarColetaRss20()
    {
        const string rss = """
            <rss version="2.0">
              <channel>
                <item>
                  <title>Documento RSS</title>
                  <link>https://exemplo.com/rss</link>
                  <description>Conteúdo</description>
                </item>
              </channel>
            </rss>
            """;
        var collector = CriarCollector(rss);

        var documents = await collector.ColetarAsync(
            "https://exemplo.com/feed",
            null,
            TestContext.Current.CancellationToken);

        documents.Should().ContainSingle();
        documents[0].Titulo.Should().Be("Documento RSS");
        documents[0].Tipo.Should().Be("RSS");
    }

    [Fact]
    public async Task ColetarAsync_DevePreservarColetaRss10()
    {
        const string rss = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns="http://purl.org/rss/1.0/">
              <channel rdf:about="https://exemplo.com/">
                <title>Feed RSS 1.0</title>
                <link>https://exemplo.com/</link>
                <description>Feed</description>
              </channel>
              <item rdf:about="https://exemplo.com/item">
                <title>Documento RDF</title>
                <link>https://exemplo.com/item</link>
                <description>Conteúdo</description>
              </item>
            </rdf:RDF>
            """;
        var collector = CriarCollector(rss);

        var documents = await collector.ColetarAsync(
            "https://exemplo.com/feed",
            null,
            TestContext.Current.CancellationToken);

        documents.Should().ContainSingle();
        documents[0].Titulo.Should().Be("Documento RDF");
        documents[0].Tipo.Should().Be("RSS");
    }

    [Fact]
    public async Task ColetarAsync_DevePreservarColetaAtom()
    {
        const string atom = """
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title>Documento Atom</title>
                <link href="https://exemplo.com/atom" />
                <summary>Conteúdo</summary>
              </entry>
            </feed>
            """;
        var collector = CriarCollector(atom);

        var documents = await collector.ColetarAsync(
            "https://exemplo.com/feed",
            null,
            TestContext.Current.CancellationToken);

        documents.Should().ContainSingle();
        documents[0].Titulo.Should().Be("Documento Atom");
        documents[0].Tipo.Should().Be("ATOM");
    }

    [Fact]
    public async Task ColetarAsync_DevePreservarAtomSemNamespace()
    {
        const string atom = """
            <feed>
              <entry>
                <title>Documento Atom sem namespace</title>
                <link href="https://exemplo.com/atom" />
                <summary>Conteúdo</summary>
              </entry>
            </feed>
            """;
        var collector = CriarCollector(atom);

        var documents = await collector.ColetarAsync(
            "https://exemplo.com/feed",
            null,
            TestContext.Current.CancellationToken);

        documents.Should().ContainSingle();
        documents[0].Titulo.Should().Be("Documento Atom sem namespace");
        documents[0].Tipo.Should().Be("ATOM");
    }

    [Fact]
    public async Task ColetarAsync_DevePreservarFallbackRssParaXmlNaoAtom()
    {
        var collector = CriarCollector("<documento />");

        var documents = await collector.ColetarAsync(
            "https://exemplo.com/feed",
            null,
            TestContext.Current.CancellationToken);

        documents.Should().BeEmpty();
    }

    private static RssAtomCollector CriarCollector(string content)
    {
        var handler = new StubHttpMessageHandler(content);
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        return new RssAtomCollector(client);
    }

    private sealed class StubHttpMessageHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8)
            });
    }
}
