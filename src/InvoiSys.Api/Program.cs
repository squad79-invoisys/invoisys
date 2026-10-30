using InvoiSys.Api.Configurations;
using InvoiSys.Api.Workers;
using InvoiSys.Infrastructure.Data;
using InvoiSys.Infrastructure.Identity;
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

if (builder.Configuration.GetValue("ColetaAutomatica:Enabled", true))
    builder.Services.AddHostedService<ColetaAutomaticaWorker>();

builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(45));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

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
