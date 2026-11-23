using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Collectors.RssAtom;

public sealed partial class RssAtomCollector(
    HttpClient httpClient,
    ILogger<RssAtomCollector> logger) : IContentCollector
{
    public bool Suporta(TipoFonte tipo) =>
        tipo is TipoFonte.Rss or TipoFonte.Atom;

    public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        string? seletorConteudo,
        CancellationToken cancellationToken)
    {
        // O seletor de conteúdo não se aplica a feeds: o formato já delimita cada item.
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var xml = await XDocument.LoadAsync(
            stream,
            LoadOptions.None,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var ehAtom = FeedDocumentParser.EhAtom(xml);
        var documentos = ehAtom
            ? LerAtom(xml, url, cancellationToken)
            : LerRss(xml, url, cancellationToken);

        logger.LogInformation(
            "Feed {Formato} lido: {Quantidade} item(ns)",
            ehAtom ? "Atom" : "RSS", documentos.Count);

        return documentos;
    }

    private static IReadOnlyList<CollectedDocument> LerRss(
        XDocument document,
        string origem,
        CancellationToken cancellationToken)
    {
        return document
            .Descendants()
            .Where(element =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return element.Name.LocalName == "item";
            })
            .Select(item =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var titulo = Valor(item, "title") ?? "Sem título";
                var link = Valor(item, "link") ?? origem;
                var descricao = RemoverHtml(Valor(item, "description"), cancellationToken);
                var data = ParseDate(Valor(item, "pubDate"));
                var metadados = JsonSerializer.Serialize(new
                {
                    guid = Valor(item, "guid"),
                    categoria = Valor(item, "category"),
                    anexos = ExtrairAnexosRss(item, origem)
                });

                cancellationToken.ThrowIfCancellationRequested();
                return CriarDocumento(
                    titulo,
                    link,
                    "RSS",
                    origem,
                    data,
                    descricao,
                    metadados);
            })
            .ToList();
    }

    private static IReadOnlyList<CollectedDocument> LerAtom(
        XDocument document,
        string origem,
        CancellationToken cancellationToken)
    {
        return document
            .Descendants()
            .Where(element =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return element.Name.LocalName == "entry";
            })
            .Select(entry =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var titulo = Valor(entry, "title") ?? "Sem título";
                var link = LinkPrincipalAtom(entry) ?? origem;
                var conteudo = RemoverHtml(
                    Valor(entry, "content") ??
                    Valor(entry, "summary"), cancellationToken);
                var data = ParseDate(
                    Valor(entry, "published") ??
                    Valor(entry, "updated"));
                var metadados = JsonSerializer.Serialize(new
                {
                    id = Valor(entry, "id"),
                    atualizadoEm = Valor(entry, "updated"),
                    anexos = ExtrairAnexosAtom(entry, origem)
                });

                cancellationToken.ThrowIfCancellationRequested();
                return CriarDocumento(
                    titulo,
                    link,
                    "ATOM",
                    origem,
                    data,
                    conteudo,
                    metadados);
            })
            .ToList();
    }

    // Arquivos que o próprio feed declara (enclosure), guardados nos metadados.
    // Sem enclosure o resultado é nulo; os links da página são buscados depois,
    // sob demanda, quando alguém abre o documento.
    private static List<object>? ExtrairAnexosRss(XElement item, string origem)
    {
        var anexos = item.Elements()
            .Where(element => string.Equals(
                element.Name.LocalName, "enclosure", StringComparison.OrdinalIgnoreCase))
            .Select(element => CriarAnexo(
                element.Attribute("url")?.Value,
                null,
                element.Attribute("type")?.Value,
                origem))
            .OfType<object>()
            .ToList();

        return anexos.Count == 0 ? null : anexos;
    }

    private static List<object>? ExtrairAnexosAtom(XElement entry, string origem)
    {
        var anexos = entry.Elements()
            .Where(element => element.Name.LocalName == "link" &&
                string.Equals(
                    element.Attribute("rel")?.Value,
                    "enclosure",
                    StringComparison.OrdinalIgnoreCase))
            .Select(element => CriarAnexo(
                element.Attribute("href")?.Value,
                element.Attribute("title")?.Value,
                element.Attribute("type")?.Value,
                origem))
            .OfType<object>()
            .ToList();

        return anexos.Count == 0 ? null : anexos;
    }

    // O link principal é o "alternate" (ou sem rel); o enclosure é um anexo.
    private static string? LinkPrincipalAtom(XElement entry)
    {
        var links = entry.Elements()
            .Where(element => element.Name.LocalName == "link")
            .ToList();

        var principal = links.FirstOrDefault(link =>
            string.IsNullOrWhiteSpace(link.Attribute("rel")?.Value) ||
            string.Equals(link.Attribute("rel")!.Value, "alternate", StringComparison.OrdinalIgnoreCase));

        return (principal ?? links.FirstOrDefault())?.Attribute("href")?.Value;
    }

    private static object? CriarAnexo(string? url, string? nome, string? tipo, string origem)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(origem, UriKind.Absolute, out var baseUri) ||
            !Uri.TryCreate(baseUri, url.Trim(), out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        return new
        {
            url = uri.AbsoluteUri,
            nome = string.IsNullOrWhiteSpace(nome) ? null : nome.Trim(),
            tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim()
        };
    }

    private static CollectedDocument CriarDocumento(
        string titulo,
        string urlOriginal,
        string tipo,
        string origem,
        DateTimeOffset? dataPublicacao,
        string? conteudo,
        string? metadados)
    {
        var hashBase = $"{titulo}|{urlOriginal}|{conteudo}";
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(hashBase)));

        return new CollectedDocument(
            titulo,
            urlOriginal,
            tipo,
            origem,
            dataPublicacao,
            conteudo,
            metadados,
            hash);
    }

    private static string? Valor(XElement parent, string localName) =>
        parent.Elements()
            .FirstOrDefault(element =>
                string.Equals(
                    element.Name.LocalName,
                    localName,
                    StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var date)
            ? date.ToUniversalTime()
            : null;

    private static string? RemoverHtml(string? value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var semTags = HtmlTagRegex().Replace(value, " ");
        cancellationToken.ThrowIfCancellationRequested();
        return Regex.Replace(semTags, @"\s+", " ").Trim();
    }

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();
}