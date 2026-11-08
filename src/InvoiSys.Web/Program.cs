using InvoiSys.Web;
using InvoiSys.Web.Authentication;
using InvoiSys.Web.Clients;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Aceita URL absoluta (API em outra origem, como no desenvolvimento) ou
// relativa, como "/", quando um proxy serve a tela e a API na mesma origem.
var apiBaseUrl = new Uri(
    new Uri(builder.HostEnvironment.BaseAddress),
    builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7101/");

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<ApiAuthenticationStateProvider>());

builder.Services.AddTransient<BrowserCredentialsHandler>();
builder.Services.AddTransient<AuthorizedHttpMessageHandler>();
builder.Services.AddScoped<AuthClient>();

builder.Services
    .AddRefitClient<IAuthApi>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<BrowserCredentialsHandler>();

builder.Services
    .AddRefitClient<IFontesClient>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

builder.Services
    .AddRefitClient<IColetasClient>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

builder.Services
    .AddRefitClient<IDocumentosClient>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

builder.Services
    .AddRefitClient<IUsuariosClient>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

builder.Services
    .AddRefitClient<IDashboardClient>()
    .ConfigureHttpClient(client =>
        client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

var host = builder.Build();

await host.Services
    .GetRequiredService<AuthClient>()
    .TentarRestaurarSessaoAsync();

await host.RunAsync();
