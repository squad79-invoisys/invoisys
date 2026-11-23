namespace InvoiSys.Application.Common.Anexos;

/// <summary>
/// Regras sobre o que é um arquivo anexável: extensões reconhecidas, tipo de
/// conteúdo confiável (nunca o informado pelo servidor de origem) e quais
/// arquivos o navegador pode exibir sem risco.
/// </summary>
public static class TiposArquivo
{
    public const string Binario = "application/octet-stream";

    // Lista fixa em vez de Path.GetInvalidFileNameChars(): no Linux, onde a API
    // roda, esse método devolve só '\0' e '/', e no Windows devolve 41 caracteres.
    private static readonly char[] InvalidosEmNomeDeArquivo =
        ['\0', '/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// Troca por "_" o que não pode ir num nome de arquivo, com o mesmo
    /// resultado em qualquer sistema operacional.
    /// </summary>
    public static string TrocarCaracteresInvalidos(string nome) =>
        new(nome
            .Select(caractere =>
                InvalidosEmNomeDeArquivo.Contains(caractere) || char.IsControl(caractere)
                    ? '_'
                    : caractere)
            .ToArray());

    // Servidores como o Plone (gov.br) expõem o arquivo em ".../arquivo.pdf/@@download/file".
    private static readonly string[] SegmentosDeDownload = ["@@download", "@@display-file"];

    private static readonly Dictionary<string, string> PorExtensao =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
            [".odp"] = "application/vnd.oasis.opendocument.presentation",
            [".rtf"] = "application/rtf",
            [".csv"] = "text/csv",
            [".zip"] = "application/zip",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

    // SVG e HTML ficam de fora de propósito: executam script quando abertos
    // como blob na origem da aplicação.
    private static readonly HashSet<string> Visualizaveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/png", "image/jpeg", "image/gif", "image/webp"
    };

    public static bool EhVisualizavel(string tipoConteudo) =>
        Visualizaveis.Contains(NormalizarTipo(tipoConteudo) ?? string.Empty);

    /// <summary>Extensão (com ponto) quando reconhecida; caso contrário, nulo.</summary>
    public static string? ExtensaoReconhecida(string? nomeOuExtensao)
    {
        if (string.IsNullOrWhiteSpace(nomeOuExtensao))
            return null;

        var extensao = Path.GetExtension(nomeOuExtensao.Trim());
        return PorExtensao.ContainsKey(extensao) ? extensao.ToLowerInvariant() : null;
    }

    /// <summary>
    /// Procura a extensão nos segmentos do caminho, do fim para o começo, para
    /// que "arquivo.pdf/@@download/file" resulte em ".pdf".
    /// </summary>
    public static string? ExtensaoDaUrl(Uri uri)
    {
        var segmentos = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = segmentos.Length - 1; i >= 0; i--)
        {
            var extensao = ExtensaoReconhecida(Uri.UnescapeDataString(segmentos[i]));
            if (extensao is not null)
                return extensao;
        }

        return null;
    }

    /// <summary>
    /// Indica se o link aponta para os bytes de um arquivo. "arquivo.pdf/view"
    /// é uma página que exibe o arquivo, não o arquivo, e por isso não conta.
    /// </summary>
    public static bool EhLinkDeArquivo(Uri uri)
    {
        var segmentos = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length == 0)
            return false;

        if (ExtensaoReconhecida(Uri.UnescapeDataString(segmentos[^1])) is not null)
            return true;

        return segmentos.Any(segmento => SegmentosDeDownload.Contains(
                segmento, StringComparer.OrdinalIgnoreCase))
            && ExtensaoDaUrl(uri) is not null;
    }

    /// <summary>
    /// Tipo de conteúdo a usar para um arquivo. A extensão vence o tipo informado
    /// pelo servidor; sem extensão reconhecida, só um tipo conhecido é aceito.
    /// </summary>
    public static string Resolver(string? extensao, string? tipoInformado)
    {
        if (extensao is not null && PorExtensao.TryGetValue(extensao, out var porExtensao))
            return porExtensao;

        var tipo = NormalizarTipo(tipoInformado);
        return tipo is not null && PorExtensao.Values.Contains(tipo, StringComparer.OrdinalIgnoreCase)
            ? tipo.ToLowerInvariant()
            : Binario;
    }

    public static bool EhTipoConhecido(string? tipoInformado)
    {
        var tipo = NormalizarTipo(tipoInformado);
        return tipo is not null && PorExtensao.Values.Contains(tipo, StringComparer.OrdinalIgnoreCase);
    }

    public static string? ExtensaoDoTipo(string? tipoInformado)
    {
        var tipo = NormalizarTipo(tipoInformado);
        if (tipo is null)
            return null;

        return PorExtensao.FirstOrDefault(par =>
            string.Equals(par.Value, tipo, StringComparison.OrdinalIgnoreCase)).Key;
    }

    private static string? NormalizarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;

        var semParametros = tipo.Split(';')[0].Trim();
        return semParametros.Length == 0 ? null : semParametros;
    }
}
