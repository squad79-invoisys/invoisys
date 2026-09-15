using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Collectors.RssAtom;

public sealed partial class RssAtomCollector(
    HttpClient httpClient) : IContentCollector
{
    public bool Suporta(TipoFonte tipo) =>
        tipo is TipoFonte.Rss or TipoFonte.Atom;

    public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var xml = await XDocument.LoadAsync(
            stream,
            LoadOptions.None,
            cancellationToken);

        return EhAtom(xml)
            ? LerAtom(xml, url)
            : LerRss(xml, url);
    }

    private static bool EhAtom(XDocument document) =>
        string.Equals(document.Root?.Name.LocalName, "feed", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CollectedDocument> LerRss(
        XDocument document,
        string origem)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "item")
            .Select(item =>
            {
                var titulo = Valor(item, "title") ?? "Sem título";
                var link = Valor(item, "link") ?? origem;
                var descricao = RemoverHtml(Valor(item, "description"));
                var data = ParseDate(Valor(item, "pubDate"));
                var metadados = JsonSerializer.Serialize(new
                {
                    guid = Valor(item, "guid"),
                    categoria = Valor(item, "category")
                });

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
        string origem)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "entry")
            .Select(entry =>
            {
                var titulo = Valor(entry, "title") ?? "Sem título";
                var link = entry.Elements()
                    .FirstOrDefault(element => element.Name.LocalName == "link")
                    ?.Attribute("href")
                    ?.Value ?? origem;
                var conteudo = RemoverHtml(
                    Valor(entry, "content") ??
                    Valor(entry, "summary"));
                var data = ParseDate(
                    Valor(entry, "published") ??
                    Valor(entry, "updated"));
                var metadados = JsonSerializer.Serialize(new
                {
                    id = Valor(entry, "id"),
                    atualizadoEm = Valor(entry, "updated")
                });

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
        DateTimeOffset.TryParse(value, out var date) ? date : null;

    private static string? RemoverHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var semTags = HtmlTagRegex().Replace(value, " ");
        return Regex.Replace(semTags, @"\s+", " ").Trim();
    }

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();
}
