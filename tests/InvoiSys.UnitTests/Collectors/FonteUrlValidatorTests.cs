using System.Net;
using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Collectors.Validation;
using InvoiSys.Domain.Enums;

namespace InvoiSys.UnitTests.Collectors;

public sealed class FonteUrlValidatorTests
{
    private const string Rss20 = """
        <?xml version="1.0"?>
        <rss version="2.0">
          <channel><title>Notícias</title></channel>
        </rss>
        """;

    private const string Rss10 = """
        <?xml version="1.0"?>
        <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                 xmlns="http://purl.org/rss/1.0/">
          <channel rdf:about="https://exemplo.com/">
            <title>Notícias</title>
            <link>https://exemplo.com/</link>
            <description>Feed</description>
          </channel>
        </rdf:RDF>
        """;

    private const string Atom = """
        <?xml version="1.0"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Notícias</title>
        </feed>
        """;

    [Theory]
    [InlineData("http://exemplo.com")]
    [InlineData("https://exemplo.com")]
    public async Task ValidarAsync_DeveAceitarWebHtmlAcessivel(string url)
    {
        var validator = CriarValidator(HttpStatusCode.OK, "<html></html>");

        var act = () => validator.ValidarAsync(
            url,
            TipoFonte.WebHtml,
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(Rss20)]
    [InlineData(Rss10)]
    public async Task ValidarAsync_DeveAceitarRssSuportado(string content)
    {
        var validator = CriarValidator(HttpStatusCode.OK, content);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidarAsync_DeveAceitarAtomValido()
    {
        var validator = CriarValidator(HttpStatusCode.OK, Atom);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Atom,
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("<feed><title>Conteúdo qualquer</title></feed>")]
    [InlineData("<feed xmlns=\"https://exemplo.com/outro\"><title>Conteúdo qualquer</title></feed>")]
    public async Task ValidarAsync_DeveRejeitarAtomSemNamespaceOficial(
        string content)
    {
        var validator = CriarValidator(HttpStatusCode.OK, content);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Atom,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("O conteúdo da URL não é um feed Atom válido");
    }

    [Theory]
    [InlineData(TipoFonte.Rss, "<documento />", "O conteúdo da URL não é um feed RSS válido")]
    [InlineData(TipoFonte.Atom, "<documento />", "O conteúdo da URL não é um feed Atom válido")]
    [InlineData(TipoFonte.Rss, Atom, "A URL contém um feed Atom, mas o tipo selecionado é RSS")]
    [InlineData(TipoFonte.Atom, Rss20, "A URL contém um feed RSS, mas o tipo selecionado é Atom")]
    public async Task ValidarAsync_DeveRejeitarConteudoIncompativel(
        TipoFonte tipo,
        string content,
        string mensagem)
    {
        var validator = CriarValidator(HttpStatusCode.OK, content);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            tipo,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage(mensagem);
    }

    [Theory]
    [InlineData("", "A URL retornou conteúdo vazio")]
    [InlineData("   ", "A URL retornou conteúdo vazio")]
    [InlineData("<rss>", "O conteúdo retornado pela URL não é um XML válido")]
    public async Task ValidarAsync_DeveRejeitarConteudoVazioOuXmlMalformado(
        string content,
        string mensagem)
    {
        var validator = CriarValidator(HttpStatusCode.OK, content);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage(mensagem);
    }

    [Fact]
    public async Task ValidarAsync_DeveRejeitarRespostaHttpSemSucesso()
    {
        var validator = CriarValidator(HttpStatusCode.BadGateway, "erro");

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("A URL retornou uma resposta HTTP sem sucesso");
    }

    [Fact]
    public async Task ValidarAsync_DeveConverterFalhaDeConexaoEmErroDeNegocio()
    {
        var validator = CriarValidator((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException()));

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Não foi possível acessar a URL informada");
    }

    [Fact]
    public async Task ValidarAsync_DeveConverterTimeoutEmErroDeNegocio()
    {
        var validator = CriarValidator((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException()));

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("A URL não respondeu no tempo esperado");
    }

    [Fact]
    public async Task ValidarAsync_DevePropagarCancelamentoDaApi()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var validator = CriarValidator(HttpStatusCode.OK, Rss20);

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            cancellationSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ValidarAsync_DeveConverterFalhaDeLeituraEmErroDeNegocio()
    {
        var validator = CriarValidator((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new BrokenContent()
            }));

        var act = () => validator.ValidarAsync(
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<BusinessException>()
            .WithMessage("Não foi possível ler o conteúdo retornado pela URL");
    }

    private static FonteUrlValidator CriarValidator(
        HttpStatusCode statusCode,
        string content) =>
        CriarValidator((_, _) => Task.FromResult(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8)
            }));

    private static FonteUrlValidator CriarValidator(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(sendAsync))
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        return new FonteUrlValidator(httpClient);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            sendAsync(request, cancellationToken);
    }

    private sealed class BrokenContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            Task.FromException(new IOException("Falha de leitura"));

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
