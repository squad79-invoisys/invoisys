using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace InvoiSys.Web.Authentication;

public sealed class AuthorizedHttpMessageHandler(
    TokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        if (!string.IsNullOrWhiteSpace(tokenStore.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokenStore.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
