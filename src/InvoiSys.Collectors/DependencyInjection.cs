using InvoiSys.Application.Common.Anexos;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Collectors.Anexos;
using InvoiSys.Collectors.Http;
using InvoiSys.Collectors.Pdf;
using InvoiSys.Collectors.RssAtom;
using InvoiSys.Collectors.Validation;
using InvoiSys.Collectors.WebHtml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Collectors;

public static class DependencyInjection
{
    public static IServiceCollection AddCollectors(this IServiceCollection services)
    {
        services.AddHttpClient<RssAtomCollector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
        });

        services.AddHttpClient<WebHtmlCollector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
        });

        services.AddHttpClient<PdfCollector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
        });

        services.AddHttpClient<IFonteUrlValidator, FonteUrlValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
        });

        // Estes dois serviços buscam endereços vindos de conteúdo coletado e são
        // acionados por usuários, então só alcançam a internet pública
        // (ManipuladorSeguro). Usam HttpClient próprio, fora do IHttpClientFactory,
        // para não herdar as tentativas automáticas do ServiceDefaults: uma recusa
        // (destino bloqueado, 403) não muda ao repetir, e o usuário espera uma
        // resposta rápida e pode tentar de novo.
        services.AddSingleton<IAnexoDescobridor>(provider => new AnexoDescobridor(
            CriarClienteSeguro(),
            provider.GetRequiredService<ILogger<AnexoDescobridor>>()));
        services.AddSingleton<IAnexoDownloader>(_ => new AnexoDownloader(CriarClienteSeguro()));

        services.AddScoped<IContentCollector>(provider =>
            provider.GetRequiredService<RssAtomCollector>());
        services.AddScoped<IContentCollector>(provider =>
            provider.GetRequiredService<WebHtmlCollector>());
        services.AddScoped<IContentCollector>(provider =>
            provider.GetRequiredService<PdfCollector>());
        services.AddScoped<ICollectorResolver, CollectorResolver>();

        return services;
    }

    private static HttpClient CriarClienteSeguro()
    {
        var cliente = new HttpClient(ManipuladorSeguro.Criar())
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");

        return cliente;
    }
}
