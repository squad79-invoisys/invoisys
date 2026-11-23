using System.Net;
using System.Text;

namespace InvoiSys.UnitTests.Collectors;

internal sealed class RespostaStubHandler(
    Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<Uri> Requisicoes { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requisicoes.Add(request.RequestUri!);
        var resposta = responder(request);
        resposta.RequestMessage ??= request;
        return Task.FromResult(resposta);
    }
}

internal static class RespostasStub
{
    public static HttpResponseMessage Html(string html) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        };

    public static HttpResponseMessage Arquivo(
        byte[] conteudo,
        string tipo = "application/pdf",
        string? disposicao = null)
    {
        var resposta = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(conteudo)
        };
        resposta.Content.Headers.ContentType = new(tipo);

        if (disposicao is not null)
            resposta.Content.Headers.TryAddWithoutValidation("Content-Disposition", disposicao);

        return resposta;
    }
}
