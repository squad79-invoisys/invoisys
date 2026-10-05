using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Collectors.WebHtml;

public sealed partial class WebHtmlCollector(
    HttpClient httpClient) : IContentCollector
{
    private const string SeletoresDescartados =
        "script, style, nav, footer, noscript, svg, iframe, template";

    private static readonly string[] SeletoresConteudo = ["article", "main", "body"];

    public bool Suporta(TipoFonte tipo) =>
        tipo is TipoFonte.WebHtml;

    public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(stream, cancellationToken);

        var titulo = ExtrairTitulo(document);
        var dataPublicacao = ExtrairDataPublicacao(document);
        var metadados = ExtrairMetadados(document);

        DescartarElementosSemConteudo(document);
        var conteudo = ExtrairConteudo(document);

        return [CriarDocumento(titulo, url, dataPublicacao, conteudo, metadados)];
    }

    private static string ExtrairTitulo(IDocument document)
    {
        var titulo = Normalizar(document.Title);

        if (string.IsNullOrWhiteSpace(titulo))
            titulo = Normalizar(document.QuerySelector("h1")?.TextContent);

        return string.IsNullOrWhiteSpace(titulo) ? "Sem título" : titulo;
    }

    private static void DescartarElementosSemConteudo(IDocument document)
    {
        foreach (var elemento in document.QuerySelectorAll(SeletoresDescartados))
            elemento.Remove();
    }

    private static string? ExtrairConteudo(IDocument document)
    {
        foreach (var seletor in SeletoresConteudo)
        {
            var elemento = document.QuerySelector(seletor);
            var texto = Normalizar(elemento?.TextContent);

            if (!string.IsNullOrWhiteSpace(texto))
                return texto;
        }

        return null;
    }

    private static DateTimeOffset? ExtrairDataPublicacao(IDocument document)
    {
        string?[] candidatos =
        [
            Meta(document, "article:published_time"),
            Meta(document, "og:article:published_time"),
            Meta(document, "date"),
            Meta(document, "DC.date.issued"),
            document.QuerySelector("time[datetime]")?.GetAttribute("datetime")
        ];

        foreach (var candidato in candidatos)
        {
            if (DateTimeOffset.TryParse(candidato, out var data))
                return data.ToUniversalTime();
        }

        return null;
    }

    private static string ExtrairMetadados(IDocument document) =>
        JsonSerializer.Serialize(new
        {
            descricao = Meta(document, "description"),
            idioma = document.DocumentElement?.GetAttribute("lang"),
            canonica = document.QuerySelector("link[rel=canonical]")?.GetAttribute("href")
        });

    private static string? Meta(IDocument document, string nome) =>
        Normalizar(
            document.QuerySelector($"meta[name='{nome}']")?.GetAttribute("content") ??
            document.QuerySelector($"meta[property='{nome}']")?.GetAttribute("content"));

    private static CollectedDocument CriarDocumento(
        string titulo,
        string urlOriginal,
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
            "WEB",
            urlOriginal,
            dataPublicacao,
            conteudo,
            metadados,
            hash);
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : EspacosRegex().Replace(valor, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();
}
