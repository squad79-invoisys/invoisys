using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvoiSys.Application.Common.Anexos;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Application.Documentos.Anexos;

/// <summary>
/// Reúne os anexos de um documento: os declarados pela fonte na coleta
/// (enclosure de RSS/Atom, guardados nos metadados) e os encontrados na página
/// de origem. A consulta é feita sob demanda, o que também cobre documentos
/// coletados antes de existir esta funcionalidade.
/// </summary>
public sealed class AnexoCatalogo(IAnexoDescobridor descobridor)
{
    public const int LimiteAnexos = 20;
    private const int LimiteNomeArquivo = 150;

    public async Task<AnexosDocumentoResponse> ListarAsync(
        Documento documento,
        CancellationToken cancellationToken)
    {
        var candidatos = LerAnexosDosMetadados(documento.Metadados).ToList();

        var descoberta = await descobridor.DescobrirAsync(
            documento.UrlOriginal,
            cancellationToken);
        candidatos.AddRange(descoberta.Anexos);

        var itens = candidatos
            .Select(Montar)
            .OfType<AnexoResponse>()
            .DistinctBy(anexo => anexo.Url, StringComparer.Ordinal)
            .Take(LimiteAnexos)
            .ToList();

        return new AnexosDocumentoResponse(itens, descoberta.Completa);
    }

    /// <summary>
    /// O identificador deriva da URL: o cliente nunca informa uma URL à API, só
    /// escolhe entre os anexos que a própria API listou para o documento.
    /// </summary>
    public static string GerarId(string url) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16]
            .ToLowerInvariant();

    internal static AnexoResponse? Montar(AnexoDescoberto descoberto)
    {
        if (!Uri.TryCreate(descoberto.Url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var url = uri.GetLeftPart(UriPartial.Query);
        var extensao = TiposArquivo.ExtensaoDaUrl(uri)
            ?? TiposArquivo.ExtensaoReconhecida(descoberto.Nome)
            ?? TiposArquivo.ExtensaoDoTipo(descoberto.TipoConteudo);
        var tipo = TiposArquivo.Resolver(extensao, descoberto.TipoConteudo);

        var nome = Limpar(descoberto.Nome) ?? NomeDaUrl(uri);

        return new AnexoResponse(
            GerarId(url),
            nome,
            MontarNomeArquivo(nome, extensao),
            url,
            extensao ?? string.Empty,
            tipo,
            TiposArquivo.EhVisualizavel(tipo));
    }

    private static IEnumerable<AnexoDescoberto> LerAnexosDosMetadados(string? metadados)
    {
        if (string.IsNullOrWhiteSpace(metadados))
            yield break;

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(metadados);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (json)
        {
            if (json.RootElement.ValueKind != JsonValueKind.Object ||
                !json.RootElement.TryGetProperty("anexos", out var anexos) ||
                anexos.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var item in anexos.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var url = Texto(item, "url");
                if (url is null)
                    continue;

                yield return new AnexoDescoberto(url, Texto(item, "nome"), Texto(item, "tipo"));
            }
        }
    }

    private static string? Texto(JsonElement objeto, string propriedade) =>
        objeto.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? Limpar(valor.GetString())
            : null;

    private static string? Limpar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var limpo = string.Join(' ', texto.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return limpo.Length <= 200 ? limpo : limpo[..200];
    }

    // O último segmento pode ser só "file" (".../arquivo.pdf/@@download/file").
    private static string NomeDaUrl(Uri uri)
    {
        var segmentos = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var comExtensao = segmentos.LastOrDefault(
            segmento => TiposArquivo.ExtensaoReconhecida(segmento) is not null);

        return Limpar(comExtensao ?? segmentos.LastOrDefault()) ?? "Anexo";
    }

    private static string MontarNomeArquivo(string nome, string? extensao)
    {
        var seguro = TiposArquivo.TrocarCaracteresInvalidos(nome).Trim(' ', '.');

        if (seguro.Length == 0)
            seguro = "anexo";

        if (extensao is not null && !seguro.EndsWith(extensao, StringComparison.OrdinalIgnoreCase))
            seguro = $"{(seguro.Length > LimiteNomeArquivo ? seguro[..LimiteNomeArquivo] : seguro)}{extensao}";

        return seguro.Length <= LimiteNomeArquivo + 10 ? seguro : seguro[..(LimiteNomeArquivo + 10)];
    }
}
