using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Collectors.RssAtom;
using InvoiSys.Collectors.Validation;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHttpClient<IFonteUrlValidator, FonteUrlValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
        });

        services.AddScoped<IContentCollector>(provider =>
            provider.GetRequiredService<RssAtomCollector>());
        services.AddScoped<ICollectorResolver, CollectorResolver>();

        return services;
    }
}
