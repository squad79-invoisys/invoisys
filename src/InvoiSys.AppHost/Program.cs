var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter(
    "postgres-password",
    secret: true);

var jwtKey = builder.AddParameter(
    "jwt-key",
    secret: true);

var adminEmail = builder.AddParameter(
    "admin-email");

var adminPassword = builder.AddParameter(
    "admin-password",
    secret: true);

var postgres = builder.AddPostgres(
    "postgres",
    password: postgresPassword);

if (!string.Equals(
        builder.Configuration["UsePersistentDatabase"],
        "false",
        StringComparison.OrdinalIgnoreCase))
{
    postgres.WithDataVolume("invoisys-postgres");
}

var database = postgres.AddDatabase("invoisys");

var api = builder
    .AddProject<Projects.InvoiSys_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("BootstrapAdmin__Email", adminEmail)
    .WithEnvironment("BootstrapAdmin__Password", adminPassword);

api.WithEnvironment("ColetaAutomatica__Enabled",
    builder.Configuration["ColetaAutomatica:Enabled"] ?? "true")
    .WithEnvironment("ColetaAutomatica__IntervaloVerificacaoSegundos",
        builder.Configuration["ColetaAutomatica:IntervaloVerificacaoSegundos"] ?? "60")
    .WithEnvironment("ColetaAutomatica__ParalelismoMaximo",
        builder.Configuration["ColetaAutomatica:ParalelismoMaximo"] ?? "4")
    .WithEnvironment("ColetaAutomatica__TimeoutFonteSegundos",
        builder.Configuration["ColetaAutomatica:TimeoutFonteSegundos"] ?? "120");

builder
    .AddProject<Projects.InvoiSys_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
