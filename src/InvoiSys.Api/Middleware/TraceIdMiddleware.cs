using System.Diagnostics;

namespace InvoiSys.Api.Middleware;

public sealed class TraceIdMiddleware(
    RequestDelegate next,
    ILogger<TraceIdMiddleware> logger)
{
    public const string HeaderName = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString()
                      ?? context.TraceIdentifier;

        context.Response.Headers[HeaderName] = traceId;

        using var scope = logger.BeginScope("TraceId:{TraceId}", traceId);

        var nivel = EhHealthCheck(context.Request.Path)
            ? LogLevel.Debug
            : LogLevel.Information;

        var inicio = Stopwatch.GetTimestamp();

        logger.Log(nivel, "Requisição iniciada: {Metodo} {Caminho}",
            context.Request.Method, context.Request.Path.Value);

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Requisição falhou: {Metodo} {Caminho} após {DuracaoMs:F0} ms",
                context.Request.Method,
                context.Request.Path.Value,
                Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
            throw;
        }

        logger.Log(nivel,
            "Requisição finalizada: {Metodo} {Caminho} respondeu {StatusCode} em {DuracaoMs:F0} ms",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
    }

    private static bool EhHealthCheck(PathString path) =>
        path.StartsWithSegments("/health") || path.StartsWithSegments("/alive");
}