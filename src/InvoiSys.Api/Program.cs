using InvoiSys.Api.Configurations;
using InvoiSys.Api.Middleware;
using InvoiSys.Api.Workers;
using InvoiSys.Infrastructure.Data;
using InvoiSys.Infrastructure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddSwaggerConfiguration();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<ColetaAutomaticaOptions>()
    .Bind(builder.Configuration.GetSection(ColetaAutomaticaOptions.SectionName))
    .Validate(options => !options.Enabled ||
        (options.IntervaloVerificacaoSegundos > 0 &&
         options.ParalelismoMaximo > 0 &&
         options.TimeoutFonteSegundos > 0),
        "Os intervalos e limites da coleta automática devem ser positivos.")
    .ValidateOnStart();

// Atrás de um proxy reverso a API só recebe HTTP. Sem isso o Request.IsHttps
// fica falso e o cookie do refresh token sai sem a flag Secure.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // O proxy roda em outro container, então não cai nas redes confiáveis
    // padrão. A API não publica porta: só o proxy alcança ela.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

if (builder.Configuration.GetValue("ColetaAutomatica:Enabled", true))
    builder.Services.AddHostedService<ColetaAutomaticaWorker>();

builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(45));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<TraceIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Em desenvolvimento sempre migra. Em produção só quando pedido pela
// configuração, para que subir a API não altere o banco sem querer.
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration);
}

app.UseHttpsRedirection();
app.UseCors(CorsConfiguration.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();

public partial class Program;
