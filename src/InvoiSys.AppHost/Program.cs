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

var postgres = builder
    .AddPostgres(
        "postgres",
        password: postgresPassword)
    .WithDataVolume("invoisys-postgres");

var database = postgres.AddDatabase("invoisys");

var api = builder
    .AddProject<Projects.InvoiSys_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("BootstrapAdmin__Email", adminEmail)
    .WithEnvironment("BootstrapAdmin__Password", adminPassword);

builder
    .AddProject<Projects.InvoiSys_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
