# Arquitetura

A solução foi organizada com separação clara de responsabilidades: controllers finos, casos de uso na Application, regras no Domain, persistência na Infrastructure, respostas padronizadas e tratamento centralizado de erros.

## Projetos

- `InvoiSys.Api`: entrada HTTP, autenticação, Swagger, CORS e filtro de exceções.
- `InvoiSys.Web`: Blazor WebAssembly, páginas, contratos e clients HTTP.
- `InvoiSys.Application`: casos de uso, DTOs, validators e contratos da aplicação.
- `InvoiSys.Domain`: entidades, enums, invariantes e contratos de repositório.
- `InvoiSys.Infrastructure`: EF Core, PostgreSQL, Identity, JWT, repositórios e Unit of Work.
- `InvoiSys.Collectors`: implementações dos mecanismos de coleta. O primeiro é RSS/Atom.
- `InvoiSys.AppHost`: orquestra API, Web e PostgreSQL com Aspire.
- `InvoiSys.ServiceDefaults`: health checks, service discovery, resiliência e OpenTelemetry.

## Dependências

```text
Web -> API -> Application -> Domain
              ^             ^
              |             |
Infrastructure -------------+
Collectors -> Application + Domain
AppHost -> API + Web + PostgreSQL
```

O `Domain` não referencia ASP.NET Core, Entity Framework ou Infrastructure.

## Fluxo de uma coleta manual

1. O usuário autenticado chama `POST /api/coletas/fontes/{fonteId}/executar`.
2. O Controller chama `IExecutarColetaManualUseCase`.
3. O caso de uso consulta a Fonte e valida se está ativa.
4. `ICollectorResolver` seleciona o coletor compatível com o tipo da Fonte.
5. O coletor devolve documentos normalizados para a Application.
6. A Application cria `Documento` e `ExecucaoColeta`.
7. Repositórios e Unit of Work confirmam a persistência.
8. O Controller devolve `ApiResponse<T>`.

## Decisão adicional: Collectors

O projeto de referência não precisa dessa camada. No InvoiSys ela foi adicionada porque a origem dos dados pode variar entre RSS, Atom e Web/HTML. A separação evita colocar parsing e HTTP dentro de Controllers ou do Domain.
