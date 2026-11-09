using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    // Mesmo valor de ColetaMetrics.MeterName (Application). O ServiceDefaults
    // não referencia a Application, por isso o nome é repetido aqui.
    private const string ColetasMeterName = "InvoiSys.Coletas";

    public static TBuilder AddServiceDefaults<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(ColetasMeterName);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter =
            !string.IsNullOrWhiteSpace(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services
                .AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Só o processo: responde /alive e não depende de nenhum recurso externo.
        builder.Services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                ["live"]);

        return builder;
    }

    /// <summary>
    /// Registra a verificação do banco. Sem a tag "live", ela entra só em /health
    /// e nunca em /alive. O timeout evita que um banco lento segure a resposta.
    /// </summary>
    public static IHostApplicationBuilder AddDbContextHealthCheck<TContext>(
        this IHostApplicationBuilder builder,
        string name = "postgresql",
        TimeSpan? timeout = null)
        where TContext : DbContext
    {
        var limite = timeout ?? TimeSpan.FromSeconds(3);

        builder.Services
            .AddHealthChecks()
            .AddDbContextCheck<TContext>(
                name,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        // AddDbContextCheck não expõe timeout; ele fica no registro do check.
        builder.Services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registro in options.Registrations)
            {
                if (registro.Name == name)
                    registro.Timeout = limite;
            }
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(
        this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration =>
                registration.Tags.Contains("live")
        });

        return app;
    }
}