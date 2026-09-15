using System.Text.Json.Serialization;
using InvoiSys.Api.Filters;
using InvoiSys.Api.Security;
using InvoiSys.Application;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Collectors;
using InvoiSys.Infrastructure;

namespace InvoiSys.Api.Configurations;

public static class ApiConfiguration
{
    public static IServiceCollection AddApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControllers(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddCollectors();

        return services;
    }
}
