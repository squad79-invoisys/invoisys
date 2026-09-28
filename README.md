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
dotnet test tests/InvoiSys.IntegrationTests/InvoiSys.IntegrationTests.csproj
```

A migration `InitialCreate` já faz parte do repositório e a API a aplica automaticamente em Development. Antes de iniciar o AppHost pela primeira vez, configure os parâmetros locais sem gravar segredos no repositório:

```powershell
dotnet user-secrets set "Parameters:postgres-password" "SUA_SENHA_POSTGRES" --project src/InvoiSys.AppHost
dotnet user-secrets set "Parameters:jwt-key" "SUA_CHAVE_JWT_COM_PELO_MENOS_64_CARACTERES" --project src/InvoiSys.AppHost
dotnet user-secrets set "Parameters:admin-email" "admin@exemplo.local" --project src/InvoiSys.AppHost
dotnet user-secrets set "Parameters:admin-password" "SUA_SENHA_ADMIN" --project src/InvoiSys.AppHost
dotnet run --project src/InvoiSys.AppHost
```

## Fluxo de trabalho

Duas branches fixas: `develop`, onde o trabalho do dia a dia é integrado, e `main`, que só recebe o que já está aprovado e estável.

**Sua branch sai da `develop` e seu Pull Request volta para a `develop`.**

```bash
git checkout develop && git pull
git checkout -b feat/kan-41-cadastrar-fonte
```

| Item | Padrão |
| --- | --- |
| Branch | `<tipo>/kan-<número>-<descrição>` — `feat/kan-41-cadastrar-fonte` |
| Commit | [Conventional Commits](https://www.conventionalcommits.org/pt-br/) — `feat: adiciona filtro por status` |
| Pull Request | Um por card, título `KAN-41: feat: adiciona cadastro de fonte`, destino `develop` |
| Merge | Squash — um commit por card na `develop` |

Os tipos são `feat`, `fix`, `chore`, `docs`, `test`, `ci` e `refactor`, usados tanto na branch quanto no commit.

Na `develop`, **você mesmo aprova e faz o merge do seu PR** — não precisa esperar revisor. Em troca, rode `dotnet build` e `dotnet test` antes, porque ninguém vai revisar depois.

A promoção de `develop` para `main` é feita pela liderança, que é quem aprova esse PR. Ninguém commita direto em nenhuma das duas.

Antes do merge, atualize sua branch com `git merge develop`. O passo a passo completo, incluindo como resolver conflito, está em [CONTRIBUTING.md](CONTRIBUTING.md).

## Documentação

- [Como contribuir e fluxo de git](CONTRIBUTING.md)
- [Documentação completa e didática](docs/documentacao-completa.md)
- [Arquitetura](docs/arquitetura.md)
- [Autenticação](docs/autenticacao.md)
- [Modelo de dados](docs/modelo-dados.md)
- [Pacotes](docs/pacotes.md)
- [Execução](docs/execucao.md)
- [Validação](docs/validacao.md)

## Observação sobre testes de integração

O projeto de integração usa `Aspire.Hosting.Testing`. O smoke test sobe AppHost, PostgreSQL efêmero e API, aguarda a API ficar saudável e valida `GET /alive` com HTTP 200. Ele requer Docker Desktop ativo, mas não reutiliza o volume persistente de desenvolvimento.

> Estado validado em 21/09/2026: restore concluído, build sem warnings/erros, 10 testes unitários e 1 teste de integração aprovados com .NET SDK 10.0.401 e Docker Desktop.
