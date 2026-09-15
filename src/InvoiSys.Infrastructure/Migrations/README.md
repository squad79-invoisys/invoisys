# Migrations

A migration inicial deve ser criada após o primeiro `dotnet build` bem-sucedido:

```powershell
dotnet tool restore
dotnet ef migrations add InitialCreate `
  --project src/InvoiSys.Infrastructure `
  --startup-project src/InvoiSys.Api `
  --output-dir Migrations
```

A API executa `Database.MigrateAsync()` em ambiente Development.
