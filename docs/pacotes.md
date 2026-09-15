# Pacotes e versões

Versões escolhidas em 15/09/2026, priorizando releases estáveis compatíveis com .NET 10.

| Pacote | Versão | Uso |
| --- | ---: | --- |
| Aspire.Hosting | 13.5.3 | AppHost |
| Aspire.Hosting.PostgreSQL | 13.5.3 | PostgreSQL no Aspire |
| Aspire.Hosting.Testing | 13.5.3 | testes de integração do AppHost |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | validators + DI |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.12 | validação de JWT |
| Microsoft.AspNetCore.Components.Authorization | 10.0.12 | estado de autenticação no Blazor |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.12 | frontend Blazor WASM |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.12 | Identity persistido por EF Core |
| Microsoft.EntityFrameworkCore | 10.0.12 | ORM |
| Microsoft.EntityFrameworkCore.Design | 10.0.12 | tooling de migrations |
| Microsoft.Extensions.Http | 10.0.12 | `AddHttpClient` no class library Collectors |
| Microsoft.Extensions.Http.Resilience | 10.0.12 | resiliência HTTP no ServiceDefaults |
| Microsoft.Extensions.ServiceDiscovery | 10.0.12 | service discovery do Aspire |
| MudBlazor | 9.10.0 | componentes visuais do frontend |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | provider PostgreSQL do EF Core |
| OpenTelemetry.* | 1.18.0 | métricas, traces e exportação OTLP |
| Refit / Refit.HttpClientFactory | 15.2.0 | clients HTTP tipados |
| Swashbuckle.AspNetCore | 10.2.3 | Swagger/OpenAPI |
| Swashbuckle.AspNetCore.Annotations | 10.2.3 | documentação dos Controllers |
| FluentAssertions | 8.11.0 | assertions dos testes |
| xunit.v3 | 4.0.0 | framework de testes |
| xunit.runner.visualstudio | 4.0.0 | integração com runners/Visual Studio |
| Microsoft.NET.Test.Sdk | 18.8.1 | infraestrutura de testes |
| coverlet.collector | 10.0.1 | coleta de cobertura |

`Microsoft.Extensions.Http` foi referenciado explicitamente somente em `InvoiSys.Collectors`, pois esse projeto é uma class library que registra `AddHttpClient`. A API recebe o stack HTTP pelo shared framework do ASP.NET Core.

## SDK e runner de testes

O `global.json` fixa a base no SDK `10.0.400` e usa `rollForward: latestFeature`, permitindo utilizar um feature band mais novo do .NET 10 quando ele estiver instalado. `allowPrerelease` permanece `false`.

O runner foi configurado como `VSTest` para manter o fluxo próximo ao projeto de referência e ao uso de `xunit.runner.visualstudio`/`coverlet.collector`.
