using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace InvoiSys.IntegrationTests.Support;

/// <summary>
/// Cliente HTTP da API com (ou sem) access token. O cookie de refresh é enviado
/// manualmente, para que cada teste controle exatamente qual sessão usa.
/// </summary>
public sealed class ApiTestClient(HttpClient http, string? accessToken)
{
    public Task<HttpResponseMessage> GetAsync(
        string uri,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, uri, null, cancellationToken);

    public Task<HttpResponseMessage> PostAsync(
        string uri,
        object? body,
        CancellationToken cancellationToken,
        string? cookie = null) =>
        SendAsync(HttpMethod.Post, uri, body, cancellationToken, cookie);

    public Task<HttpResponseMessage> PutAsync(
        string uri,
        object? body,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Put, uri, body, cancellationToken);

    public Task<HttpResponseMessage> PatchAsync(
        string uri,
        object? body,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Patch, uri, body, cancellationToken);

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken,
        string? cookie = null)
    {
        using var request = new HttpRequestMessage(method, uri);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.Add("Cookie", cookie);

        if (body is not null)
        {
            request.Content = JsonContent.Create(
                body,
                body.GetType(),
                null,
                TestJson.Options);
        }

        return await http.SendAsync(request, cancellationToken);
    }
}
