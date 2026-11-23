using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using InvoiSys.Application.Common.Anexos;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Collectors.Anexos;

public sealed class AnexoDescobridor(
    HttpClient httpClient,
    ILogger<AnexoDescobridor> logger) : IAnexoDescobridor
{
    public const int LimiteAnexos = 20;
    private const int LimiteHtmlBytes = 2 * 1024 * 1024;

    // Do mais específico ao mais amplo: o menu e o rodapé do site costumam ter
    // links para PDFs que não pertencem ao documento.
    private static readonly string[] Escopos = ["article", "main", "body"];

    public async Task<ResultadoDescoberta> DescobrirAsync(
        string urlDocumento,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(urlDocumento, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new ResultadoDescoberta([], true);
        }

        try
        {
            using var resposta = await httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Busca de anexos: a página do documento respondeu {StatusCode}.",
                    (int)resposta.StatusCode);
                return new ResultadoDescoberta([], false);
            }

            var destino = resposta.RequestMessage?.RequestUri ?? uri;
            var tipo = resposta.Content.Headers.ContentType?.MediaType;

            if (EhHtml(tipo))
            {
                var anexos = await ExtrairDaPaginaAsync(resposta.Content, destino, cancellationToken);
                return new ResultadoDescoberta(anexos, true);
            }

            // A própria URL do documento é o arquivo (ex.: fonte do tipo PDF).
            if (TiposArquivo.EhTipoConhecido(tipo) || TiposArquivo.ExtensaoDaUrl(destino) is not null)
            {
                return new ResultadoDescoberta(
                    [new AnexoDescoberto(destino.GetLeftPart(UriPartial.Query), null, tipo)],
                    true);
            }

            return new ResultadoDescoberta([], true);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException ||
            (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Busca de anexos: não foi possível consultar a página do documento.");
            return new ResultadoDescoberta([], false);
        }
    }

    internal static bool EhHtml(string? tipo) =>
        tipo is not null &&
        (tipo.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
         tipo.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase));

    private static async Task<IReadOnlyList<AnexoDescoberto>> ExtrairDaPaginaAsync(
        HttpContent conteudo,
        Uri destino,
        CancellationToken cancellationToken)
    {
        await using var origem = await conteudo.ReadAsStreamAsync(cancellationToken);
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];

        // Páginas maiores que o limite são analisadas só no começo.
        while (memoria.Length < LimiteHtmlBytes)
        {
            var lidos = await origem.ReadAsync(buffer, cancellationToken);
            if (lidos == 0)
                break;

            memoria.Write(buffer, 0, lidos);
        }

        memoria.Position = 0;
        using var documento = await new HtmlParser().ParseDocumentAsync(memoria, cancellationToken);
        var baseUri = ResolverBase(documento, destino);

        foreach (var seletor in Escopos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var escopo = documento.QuerySelector(seletor);
            if (escopo is null)
                continue;

            var anexos = ExtrairLinks(escopo, baseUri);
            if (anexos.Count > 0)
                return anexos;
        }

        return [];
    }

    private static Uri ResolverBase(IDocument documento, Uri destino)
    {
        var href = documento.QuerySelector("base[href]")?.GetAttribute("href");

        return !string.IsNullOrWhiteSpace(href) &&
               Uri.TryCreate(destino, href, out var baseUri) &&
               (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps)
            ? baseUri
            : destino;
    }

    private static List<AnexoDescoberto> ExtrairLinks(IElement escopo, Uri baseUri)
    {
        var anexos = new List<AnexoDescoberto>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);

        foreach (var link in escopo.QuerySelectorAll("a[href]"))
        {
            var href = link.GetAttribute("href")?.Trim();
            if (string.IsNullOrEmpty(href) || href.StartsWith('#'))
                continue;

            if (!Uri.TryCreate(baseUri, href, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !TiposArquivo.EhLinkDeArquivo(uri))
            {
                continue;
            }

            var url = uri.GetLeftPart(UriPartial.Query);
            if (!vistos.Add(url))
                continue;

            var nome = string.Join(' ', link.TextContent.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            anexos.Add(new AnexoDescoberto(url, nome.Length == 0 ? null : nome, null));

            if (anexos.Count >= LimiteAnexos)
                break;
        }

        return anexos;
    }
}
