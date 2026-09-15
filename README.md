# InvoiSys

Plataforma web para automatizar a descoberta, coleta e centralização de informações relacionadas a documentos fiscais eletrônicos (DF-e).

> Projeto desenvolvido pelo Squad 79 a partir de um desafio proposto pela InvoiSys. Este repositório não é o repositório oficial da empresa.

## O que existe nesta base

- cadastro e gerenciamento de Fontes;
- histórico de execuções de coleta;
- armazenamento de documentos e metadados;
- primeiro conector RSS/Atom;
- estrutura preparada para Web/HTML;
- ASP.NET Core Identity + JWT + Refresh Token;
- perfis Administrador, Operador e Consulta;
- auditoria de quem cadastrou/altera Fontes e solicitou coletas manuais;
- Blazor WebAssembly + MudBlazor;
- PostgreSQL + Entity Framework Core;
- Swagger, health checks, Aspire e OpenTelemetry;
- Docker Compose;
- testes unitários e estrutura de integração com Aspire.

## Estrutura

```text
src/
├── InvoiSys.Api
├── InvoiSys.Web
├── InvoiSys.Application
├── InvoiSys.Domain
├── InvoiSys.Infrastructure
├── InvoiSys.Collectors
├── InvoiSys.AppHost
└── InvoiSys.ServiceDefaults

tests/
├── InvoiSys.UnitTests
└── InvoiSys.IntegrationTests

deploy/
docs/
```

## Arquitetura

- **Api**: Controllers, configuração HTTP, Swagger, CORS e filtro centralizado de erros.
- **Web**: Blazor WASM, páginas, contratos e clients HTTP.
- **Application**: casos de uso, DTOs, validações e abstrações.
- **Domain**: entidades, enums, invariantes e contratos de repositório.
- **Infrastructure**: PostgreSQL, EF Core, Identity, JWT, repositórios e Unit of Work.
- **Collectors**: implementação dos mecanismos de coleta externos.
- **AppHost**: orquestra API, Web e PostgreSQL com Aspire.
- **ServiceDefaults**: health checks, service discovery, resiliência e telemetria.

Os Controllers recebem/devolvem contratos e delegam para casos de uso; regra de negócio e acesso ao banco não ficam nos Controllers.

## Tecnologias

.NET 10 · ASP.NET Core · Blazor WebAssembly · MudBlazor · PostgreSQL · EF Core · Identity · JWT · FluentValidation · Swagger · Aspire · OpenTelemetry · Docker · xUnit · Refit

## Executar

Leia [docs/execucao.md](docs/execucao.md).

Resumo:

```powershell
dotnet tool restore
dotnet restore InvoiSys.sln
dotnet build InvoiSys.sln
dotnet test tests/InvoiSys.UnitTests/InvoiSys.UnitTests.csproj
```

Antes da primeira execução com banco, gere a migration `InitialCreate` conforme `docs/execucao.md`.

## Documentação

- [Documentação completa e didática](docs/documentacao-completa.md)
- [Arquitetura](docs/arquitetura.md)
- [Autenticação](docs/autenticacao.md)
- [Modelo de dados](docs/modelo-dados.md)
- [Pacotes](docs/pacotes.md)
- [Execução](docs/execucao.md)
- [Validação](docs/validacao.md)

## Observação sobre testes de integração

O projeto de integração usa `Aspire.Hosting.Testing`. O smoke test inicial está marcado como `Skip` até a criação da migration `InitialCreate`, pois a API aplica migrations e seed ao iniciar em Development.

> Estado atual: a base ainda precisa passar por `dotnet restore`, `dotnet build` e testes em um ambiente com o SDK .NET 10 e Docker antes de ser considerada uma entrega final validada.
