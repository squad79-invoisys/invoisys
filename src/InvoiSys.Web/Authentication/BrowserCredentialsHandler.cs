using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace InvoiSys.Web.Authentication;

public sealed class BrowserCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        return base.SendAsync(request, cancellationToken);
    }
}
