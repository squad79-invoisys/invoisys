using InvoiSys.Application.Common.Anexos;
using InvoiSys.Application.Common.Exceptions;

namespace InvoiSys.Collectors.Anexos;

public sealed class AnexoDownloader(HttpClient httpClient) : IAnexoDownloader
{
    public const long TamanhoMaximoBytes = 50L * 1024 * 1024;

    private static readonly TimeSpan PrazoTotal = TimeSpan.FromMinutes(2);

    public async Task<ArquivoAnexo> BaixarAsync(
        string url,
        string? nomeSugerido,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new BusinessException("O endereço do anexo é inválido.");
        }

        HttpResponseMessage? resposta = null;
        try
        {
            // GET, nunca HEAD: há servidores (como o gov.br) que recusam HEAD.
            resposta = await httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!resposta.IsSuccessStatusCode)
                throw new BusinessException("O servidor de origem recusou o download do anexo.");

            if (resposta.Content.Headers.ContentLength > TamanhoMaximoBytes)
                throw new BusinessException("O anexo excede o tamanho máximo de 50 MB.");

            var tipoInformado = resposta.Content.Headers.ContentType?.MediaType;
            if (AnexoDescobridor.EhHtml(tipoInformado))
                throw new BusinessException("O endereço do anexo retornou uma página, e não um arquivo.");

            var destino = resposta.RequestMessage?.RequestUri ?? uri;
            var nome = NomeDoArquivo(resposta, destino, nomeSugerido);
            var extensao = TiposArquivo.ExtensaoReconhecida(nome)
                ?? TiposArquivo.ExtensaoDaUrl(destino);
            var tipo = TiposArquivo.Resolver(extensao, tipoInformado);

            var fluxo = await resposta.Content.ReadAsStreamAsync(cancellationToken);
            var limitado = new StreamLimitado(fluxo, resposta, TamanhoMaximoBytes, PrazoTotal);

            return new ArquivoAnexo(limitado, tipo, nome);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException ||
            (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            resposta?.Dispose();
            throw new BusinessException("Não foi possível baixar o anexo do site de origem.");
        }
        catch
        {
            resposta?.Dispose();
            throw;
        }
    }

    private static string NomeDoArquivo(HttpResponseMessage resposta, Uri destino, string? nomeSugerido)
    {
        var disposicao = resposta.Content.Headers.ContentDisposition;
        var nome = disposicao?.FileNameStar ?? disposicao?.FileName?.Trim('"');

        if (string.IsNullOrWhiteSpace(nome))
            nome = nomeSugerido;

        if (string.IsNullOrWhiteSpace(nome))
            nome = Uri.UnescapeDataString(destino.Segments.LastOrDefault()?.Trim('/') ?? string.Empty);

        if (string.IsNullOrWhiteSpace(nome))
            nome = "anexo";

        var invalidos = Path.GetInvalidFileNameChars();
        return new string(nome
            .Select(caractere => invalidos.Contains(caractere) || char.IsControl(caractere) ? '_' : caractere)
            .ToArray())
            .Trim();
    }
}
