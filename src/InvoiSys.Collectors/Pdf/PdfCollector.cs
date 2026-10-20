using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace InvoiSys.Collectors.Pdf;

public sealed partial class PdfCollector(
    HttpClient httpClient) : IContentCollector
{
    private const string MensagemPdfInvalido =
        "O conteúdo da URL não é um arquivo PDF válido.";

    public const long TamanhoMaximoBytes = 50L * 1024 * 1024;

    private const string MensagemPdfGrande =
        "O PDF excede o tamanho máximo de 50 MB.";

    public bool Suporta(TipoFonte tipo) =>
        tipo is TipoFonte.Pdf;

    public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        string? seletorConteudo,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await LerConteudoAsync(response.Content, cancellationToken);

        if (!PdfAssinatura.Possui(bytes))
            throw new BusinessException(MensagemPdfInvalido);

        using var document = AbrirDocumento(bytes);

        var conteudo = ExtrairConteudo(document);

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            throw new BusinessException(
                "O PDF não possui texto extraível. Pode ser um documento escaneado; OCR não é suportado.");
        }

        var titulo = ExtrairTitulo(document, url);
        var dataPublicacao = document.Information.GetCreatedDateTimeOffset()?.ToUniversalTime();
        var metadados = ExtrairMetadados(document);

        return [CriarDocumento(titulo, url, dataPublicacao, conteudo, metadados)];
    }

    // Recusa pelo Content-Length quando informado e, sem ele, interrompe a
    // leitura ao passar do limite, para não carregar arquivos enormes na memória.
    private static async Task<byte[]> LerConteudoAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > TamanhoMaximoBytes)
            throw new BusinessException(MensagemPdfGrande);

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int lidos;

        while ((lidos = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memoria.Length + lidos > TamanhoMaximoBytes)
                throw new BusinessException(MensagemPdfGrande);

            memoria.Write(buffer, 0, lidos);
        }

        return memoria.ToArray();
    }

    private static PdfDocument AbrirDocumento(byte[] bytes)
    {
        try
        {
            return PdfDocument.Open(bytes);
        }
        catch (PdfDocumentEncryptedException)
        {
            throw new BusinessException(
                "O PDF está protegido por senha e não pode ser lido.");
        }
        catch (PdfDocumentFormatException)
        {
            throw new BusinessException(MensagemPdfInvalido);
        }
    }

    private static string? ExtrairConteudo(PdfDocument document)
    {
        var paginas = document.GetPages()
            .Select(pagina => Normalizar(ContentOrderTextExtractor.GetText(pagina)))
            .Where(texto => !string.IsNullOrWhiteSpace(texto));

        return Normalizar(string.Join(" ", paginas));
    }

    private static string ExtrairTitulo(PdfDocument document, string url)
    {
        var titulo = Normalizar(document.Information.Title);

        if (string.IsNullOrWhiteSpace(titulo))
            titulo = ExtrairNomeDoArquivo(url);

        return string.IsNullOrWhiteSpace(titulo) ? "Sem título" : titulo;
    }

    private static string? ExtrairNomeDoArquivo(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var nome = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));

        if (nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            nome = nome[..^".pdf".Length];

        return Normalizar(nome);
    }

    private static string ExtrairMetadados(PdfDocument document) =>
        JsonSerializer.Serialize(new
        {
            autor = Normalizar(document.Information.Author),
            assunto = Normalizar(document.Information.Subject),
            paginas = document.NumberOfPages
        });

    private static CollectedDocument CriarDocumento(
        string titulo,
        string urlOriginal,
        DateTimeOffset? dataPublicacao,
        string conteudo,
        string metadados)
    {
        var hashBase = $"{titulo}|{urlOriginal}|{conteudo}";
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(hashBase)));

        return new CollectedDocument(
            titulo,
            urlOriginal,
            "PDF",
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
