using System.Xml;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Collectors.RssAtom;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Collectors.Validation;

public sealed class FonteUrlValidator(
    HttpClient httpClient) : IFonteUrlValidator
{
    private static readonly TimeSpan ValidationTimeout =
        TimeSpan.FromSeconds(10);

    public async Task ValidarAsync(
        string url,
        TipoFonte tipo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutSource = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ValidationTimeout);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new BusinessException(
                "A URL não respondeu no tempo esperado");
        }
        catch (HttpRequestException)
        {
            throw new BusinessException(
                "Não foi possível acessar a URL informada");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new BusinessException(
                    "A URL retornou uma resposta HTTP sem sucesso");
            }

            string content;

            try
            {
                content = await response.Content.ReadAsStringAsync(
                    timeoutSource.Token);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new BusinessException(
                    "A URL não respondeu no tempo esperado");
            }
            catch (HttpRequestException)
            {
                throw new BusinessException(
                    "Não foi possível ler o conteúdo retornado pela URL");
            }
            catch (IOException)
            {
                throw new BusinessException(
                    "Não foi possível ler o conteúdo retornado pela URL");
            }

            ValidarConteudo(content, tipo);
        }
    }

    private static void ValidarConteudo(string content, TipoFonte tipo)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new BusinessException("A URL retornou conteúdo vazio");

        if (tipo == TipoFonte.WebHtml)
            return;

        try
        {
            var document = FeedDocumentParser.Parse(content);
            ValidarTipo(document, tipo);
        }
        catch (XmlException)
        {
            throw new BusinessException(
                "O conteúdo retornado pela URL não é um XML válido");
        }
    }

    private static void ValidarTipo(
        System.Xml.Linq.XDocument document,
        TipoFonte tipoEsperado)
    {
        var tipoIdentificado = FeedDocumentParser.IdentificarTipoValido(document);

        if (tipoIdentificado == tipoEsperado)
            return;

        if (tipoIdentificado == TipoFonte.Rss && tipoEsperado == TipoFonte.Atom)
        {
            throw new BusinessException(
                "A URL contém um feed RSS, mas o tipo selecionado é Atom");
        }

        if (tipoIdentificado == TipoFonte.Atom && tipoEsperado == TipoFonte.Rss)
        {
            throw new BusinessException(
                "A URL contém um feed Atom, mas o tipo selecionado é RSS");
        }

        var tipo = tipoEsperado == TipoFonte.Rss ? "RSS" : "Atom";
        throw new BusinessException(
            $"O conteúdo da URL não é um feed {tipo} válido");
    }
}
