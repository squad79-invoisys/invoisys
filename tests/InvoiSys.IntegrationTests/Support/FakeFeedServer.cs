using System.Collections.Concurrent;
using System.Security;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InvoiSys.IntegrationTests.Support;

/// <summary>
/// Servidor HTTP local que publica feeds RSS/Atom previsíveis. A API cadastra e
/// coleta Fontes a partir de URLs reais, então os testes precisam de um destino
/// que não dependa da internet.
/// </summary>
/// <remarks>
/// O parâmetro de query "token" torna o conteúdo único por teste: os títulos dos
/// documentos coletados carregam o token, o que permite filtrar a listagem sem
/// interferência de outros testes.
/// </remarks>
public sealed class FakeFeedServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeFeedServer(WebApplication app, Uri baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    /// <summary>Feed RSS 2.0 com 2 itens.</summary>
    public string RssUrl(string token) => Montar("rss.xml", token);

    /// <summary>Feed Atom com 1 entrada.</summary>
    public string AtomUrl(string token) => Montar("atom.xml", token);

    /// <summary>
    /// Responde com um feed RSS válido apenas na primeira requisição por token
    /// (a validação do cadastro) e com 404 nas seguintes (a coleta).
    /// </summary>
    public string InstavelUrl(string token) => Montar("instavel.xml", token);

    /// <summary>Página HTML simples, para Fontes do tipo WebHtml.</summary>
    public string HtmlUrl() => new Uri(BaseAddress, "pagina.html").ToString();

    /// <summary>URL que sempre responde 404.</summary>
    public string InexistenteUrl() => new Uri(BaseAddress, "inexistente.xml").ToString();

    public static async Task<FakeFeedServer> StartAsync(
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();

        var app = builder.Build();
        var acessos = new ConcurrentDictionary<string, int>();

        app.MapGet("/rss.xml", (HttpRequest request) =>
            Xml(MontarRss(BaseUrl(request), Token(request)), "application/rss+xml"));

        app.MapGet("/atom.xml", (HttpRequest request) =>
            Xml(MontarAtom(BaseUrl(request), Token(request)), "application/atom+xml"));

        app.MapGet("/instavel.xml", (HttpRequest request) =>
        {
            var token = Token(request);
            var tentativa = acessos.AddOrUpdate(token, 1, (_, atual) => atual + 1);

            return tentativa == 1
                ? Xml(MontarRss(BaseUrl(request), token), "application/rss+xml")
                : Results.NotFound();
        });

        app.MapGet("/pagina.html", () => Results.Content(
            "<html><head><title>Página de teste</title></head>" +
            "<body><article><h1>Comunicado</h1><p>Conteúdo de teste.</p></article></body></html>",
            "text/html",
            Encoding.UTF8));

        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        return new FakeFeedServer(app, new Uri(app.Urls.First()));
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private string Montar(string caminho, string token) =>
        new Uri(
            BaseAddress,
            $"{caminho}?token={Uri.EscapeDataString(token)}").ToString();

    private static string Token(HttpRequest request) =>
        request.Query["token"].FirstOrDefault() ?? "padrao";

    private static string BaseUrl(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}";

    private static IResult Xml(string conteudo, string contentType) =>
        Results.Content(conteudo, contentType, Encoding.UTF8);

    private static string MontarRss(string baseUrl, string token)
    {
        var t = SecurityElement.Escape(token);

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>Feed de teste {t}</title>
                <link>{baseUrl}</link>
                <description>Feed usado nos testes de integração</description>
                <item>
                  <title>Nota fiscal {t} A</title>
                  <link>{baseUrl}/documentos/{t}/a</link>
                  <description>Primeiro documento {t}</description>
                  <pubDate>Mon, 05 Oct 2026 10:00:00 GMT</pubDate>
                  <guid>{t}-a</guid>
                </item>
                <item>
                  <title>Nota fiscal {t} B</title>
                  <link>{baseUrl}/documentos/{t}/b</link>
                  <description>Segundo documento {t}</description>
                  <pubDate>Mon, 05 Oct 2026 11:00:00 GMT</pubDate>
                  <guid>{t}-b</guid>
                </item>
              </channel>
            </rss>
            """;
    }

    private static string MontarAtom(string baseUrl, string token)
    {
        var t = SecurityElement.Escape(token);

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <title>Feed Atom de teste {t}</title>
              <id>urn:invoisys:{t}</id>
              <updated>2026-10-05T10:00:00Z</updated>
              <entry>
                <title>Entrada Atom {t}</title>
                <link href="{baseUrl}/atom/{t}"/>
                <id>urn:invoisys:{t}:1</id>
                <updated>2026-10-05T10:00:00Z</updated>
                <summary>Resumo da entrada {t}</summary>
              </entry>
            </feed>
            """;
    }
}
