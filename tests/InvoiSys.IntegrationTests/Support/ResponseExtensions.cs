using System.Net.Http.Json;
using InvoiSys.Application.Common.Responses;

namespace InvoiSys.IntegrationTests.Support;

public static class ResponseExtensions
{
    public static async Task<ApiResponse<T>> LerApiAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var corpo = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            TestJson.Options,
            cancellationToken);

        return corpo ?? throw new InvalidOperationException(
            "A resposta da API não possui corpo JSON.");
    }

    public static async Task<T> LerDadosAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var corpo = await response.LerApiAsync<T>(cancellationToken);

        return corpo.Dados ?? throw new InvalidOperationException(
            $"A resposta da API não trouxe dados. Mensagem: {corpo.Mensagem}");
    }

    public static async Task GarantirSucessoAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var corpo = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Falha na preparação do teste: HTTP {(int)response.StatusCode}. {corpo}");
    }

    public static string? ObterSetCookie(
        this HttpResponseMessage response,
        string nomeCookie)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var valores))
            return null;

        return valores.FirstOrDefault(valor =>
            valor.StartsWith(nomeCookie + "=", StringComparison.Ordinal));
    }
    
    public static string ParaHeaderCookie(this string setCookie) =>
        setCookie.Split(';')[0].Trim();
}
