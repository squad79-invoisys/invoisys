# Execução local

## Pré-requisitos

- .NET SDK 10.0.400 ou mais recente dentro da linha 10.x.
- Docker Desktop para Aspire, PostgreSQL e testes de integração.

## 1. Validar o projeto

```powershell
dotnet --version
dotnet tool restore
dotnet restore InvoiSys.sln
dotnet build InvoiSys.sln
dotnet test tests/InvoiSys.UnitTests/InvoiSys.UnitTests.csproj
```

## 2. Criar a migration inicial

Configure temporariamente uma conexão para o design-time factory:

```powershell
$env:INVOISYS_CONNECTION_STRING="Host=localhost;Port=5432;Database=invoisys;Username=postgres;Password=SUA_SENHA"

dotnet ef migrations add InitialCreate `
  --project src/InvoiSys.Infrastructure `
  --startup-project src/InvoiSys.Api `
  --output-dir Migrations
```

## 3. Aspire

Configure os parâmetros do AppHost via user-secrets ou configuração local. Os parâmetros obrigatórios são:

- `postgres-password`
- `jwt-key`
- `admin-email`
- `admin-password`

Depois:

```powershell
dotnet run --project src/InvoiSys.AppHost
```

## 4. Docker Compose

```powershell
Copy-Item deploy/.env.example deploy/.env
# edite deploy/.env e troque as senhas/chave

docker compose --env-file deploy/.env -f deploy/compose.yaml up --build -d
```

Web: `http://localhost:5056`

Swagger: `http://localhost:8080/swagger`
