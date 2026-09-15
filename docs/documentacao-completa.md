# InvoiSys — Documentação completa e didática

> Esta documentação descreve o estado **real** da base atual do InvoiSys. Ela foi escrita para ajudar quem já conhece C# e ASP.NET a entender não só onde o código está, mas **por que ele foi separado dessa forma e como as partes trabalham juntas**.

---

# 1. Visão geral do projeto

## 1.1 O que é o InvoiSys

O InvoiSys é uma plataforma web criada para automatizar a descoberta, coleta e centralização de informações relacionadas a documentos fiscais eletrônicos (DF-e).

A ideia principal é evitar que uma pessoa precise entrar manualmente em vários sites, portais ou feeds para procurar novidades. Em vez disso, o sistema cadastra fontes, executa coletas, armazena o que encontrou e mantém um histórico do que aconteceu.

Nesta base inicial, o fluxo mais completo já implementado é o de **fontes RSS e Atom**. O tipo `WebHtml` já existe no domínio, mas o coletor de páginas HTML ainda não foi implementado.

## 1.2 Qual problema ele resolve

Sem uma plataforma como essa, a equipe precisaria:

- lembrar quais fontes consultar;
- entrar manualmente em cada uma;
- verificar se existe conteúdo novo;
- guardar links e informações;
- registrar quando a consulta aconteceu;
- descobrir depois quem executou determinada ação.

O InvoiSys começa a centralizar esse processo.

## 1.3 Objetivo do sistema

O objetivo é permitir que uma equipe:

1. cadastre fontes;
2. controle se elas estão ativas ou inativas;
3. execute coletas;
4. registre o histórico das execuções;
5. salve os documentos encontrados;
6. consulte essas informações em uma interface web;
7. controle acesso por usuário e perfil.

## 1.4 Fluxo geral

```text
Usuário
   ↓
InvoiSys.Web
   ↓
InvoiSys.Api
   ↓
InvoiSys.Application
   ↓
Domain / Infrastructure / Collectors
   ↓
PostgreSQL / fontes externas
```

Essa seta não significa que todas as camadas chamam todas as outras diretamente.

Ela representa o caminho de uma operação. O ponto mais importante é que cada parte tem uma responsabilidade.

- `Web` cuida da interface.
- `Api` recebe requisições HTTP.
- `Application` coordena os casos de uso.
- `Domain` protege as regras de negócio.
- `Infrastructure` cuida de banco, Identity e persistência.
- `Collectors` acessa fontes externas.
- PostgreSQL guarda os dados.

## 1.5 Exemplo real: cadastrar uma fonte RSS e executar uma coleta manual

Vamos acompanhar uma situação real.

### Parte A — cadastro da fonte

O usuário está na tela de fontes e quer cadastrar:

```text
Nome: Portal Fiscal
URL: https://exemplo.com/rss
Tipo: Rss
Periodicidade: 30 minutos
```

O caminho esperado é:

```text
Tela
  ↓
IFontesClient
  ↓
POST /api/fontes
  ↓
FontesController
  ↓
ICriarFonteUseCase
  ↓
CriarFonteUseCase
  ↓
CriarFonteValidator
  ↓
Fonte (Domain)
  ↓
IFonteRepository
  ↓
FonteRepository
  ↓
ApplicationDbContext
  ↓
IUnitOfWork
  ↓
PostgreSQL
```

O Controller não cria a entidade diretamente e também não acessa o banco.

O `CriarFonteUseCase` coordena a operação.

A entidade `Fonte` garante regras que nunca deveriam ser quebradas, por exemplo:

- nome obrigatório;
- URL HTTP/HTTPS válida;
- periodicidade maior que zero;
- usuário responsável obrigatório.

### Parte B — coleta manual

Depois, o usuário solicita uma coleta manual dessa fonte.

O caminho é:

```text
Tela
  ↓
IColetasClient
  ↓
POST /api/coletas/fontes/{fonteId}/executar
  ↓
ColetasController
  ↓
IExecutarColetaManualUseCase
  ↓
ExecutarColetaManualUseCase
  ↓
IFonteRepository
  ↓
CollectorResolver
  ↓
RssAtomCollector
  ↓
Fonte RSS externa
  ↓
CollectedDocument
  ↓
Documento (Domain)
  ↓
IDocumentoRepository
  ↓
IUnitOfWork
  ↓
PostgreSQL
```

O Use Case:

1. identifica o usuário atual;
2. busca a fonte;
3. verifica se ela está ativa;
4. cria uma `ExecucaoColeta` manual;
5. escolhe o coletor correto;
6. busca o XML da fonte;
7. transforma os itens encontrados em documentos;
8. salva os documentos;
9. conclui a execução;
10. confirma tudo no banco.

Esse exemplo mostra bem a divisão de responsabilidades do projeto.

---

# 2. Arquitetura

## 2.1 Estrutura principal

```text
src/
├── InvoiSys.Api
├── InvoiSys.Application
├── InvoiSys.Domain
├── InvoiSys.Infrastructure
├── InvoiSys.Collectors
├── InvoiSys.Web
├── InvoiSys.AppHost
└── InvoiSys.ServiceDefaults

tests/
├── InvoiSys.UnitTests
└── InvoiSys.IntegrationTests
```

## 2.2 Por que existem vários projetos

Seria possível colocar Controllers, EF Core, entidades, autenticação, interface e coleta dentro de uma única API.

No começo isso pareceria mais simples. O problema aparece quando o sistema cresce.

Se tudo ficar misturado:

- regra de negócio começa a depender de banco;
- Controller fica enorme;
- testes ficam mais difíceis;
- mudar uma tecnologia afeta várias partes;
- novos desenvolvedores têm dificuldade para descobrir onde cada código deve ficar.

A separação usada aqui tenta responder uma pergunta simples:

> "Qual é a responsabilidade deste código?"

Cada projeto tem uma resposta diferente.

## 2.3 Desenho simplificado

```text
                 ┌──────────────┐
                 │ InvoiSys.Web │
                 └──────┬───────┘
                        │ HTTP
                        ▼
                 ┌──────────────┐
                 │ InvoiSys.Api │
                 └──────┬───────┘
                        ▼
            ┌──────────────────────┐
            │ InvoiSys.Application│
            └───────┬──────────────┘
                    ▼
             ┌───────────────┐
             │ InvoiSys.Domain│
             └───────────────┘

Infrastructure ──► Application / Domain
Collectors     ──► Application / Domain

AppHost orquestra os serviços.
ServiceDefaults compartilha observabilidade e resiliência.
```

### Como ler esse desenho

O `Domain` não precisa saber que PostgreSQL existe.

Ele sabe o que é uma `Fonte`, uma `ExecucaoColeta` e um `Documento`.

O `Application` conhece o Domain e coordena o que deve acontecer.

A `Infrastructure` implementa detalhes técnicos, como:

- EF Core;
- PostgreSQL;
- Identity;
- JWT;
- repositories.

`Collectors` implementa comunicação com fontes externas.

`Api` conecta o mundo HTTP aos casos de uso.

`Web` conversa com a API.

## 2.4 Dependências corretas

Na base atual:

```text
Application → Domain

Infrastructure → Application
Infrastructure → Domain

Collectors → Application
Collectors → Domain

Api → Application
Api → Infrastructure
Api → Collectors
Api → ServiceDefaults

Web → Application

AppHost → Api
AppHost → Web
```

## 2.5 Exemplos de dependências erradas

Seria errado:

```text
Domain → Infrastructure
```

porque uma regra como "coleta manual precisa de usuário" não deveria depender do EF Core.

Também seria ruim:

```text
Domain → Api
```

porque uma entidade não deve conhecer `HttpContext`, `Controller` ou status HTTP.

Outro exemplo ruim:

```text
Controller → ApplicationDbContext
```

Isso faria a API assumir responsabilidade que pertence à Infrastructure/Application.

---

# 3. InvoiSys.Domain

## 3.1 O que é

`InvoiSys.Domain` é o núcleo das regras de negócio.

Ele não possui referência ao EF Core, Identity ou ASP.NET.

É onde ficam conceitos que continuam sendo verdade mesmo se a tecnologia mudar.

Se amanhã PostgreSQL fosse substituído, a regra:

> "Uma coleta manual precisa identificar o usuário solicitante"

continuaria existindo.

Por isso ela fica no Domain.

## 3.2 Entidade Fonte

Arquivo:

```text
src/InvoiSys.Domain/Entities/Fonte.cs
```

Uma `Fonte` representa um local que o InvoiSys pode consultar.

Exemplos:

- um feed RSS;
- um feed Atom;
- futuramente uma página Web/HTML.

### Propriedades principais

| Propriedade | Significado |
|---|---|
| `Id` | identificador único |
| `Nome` | nome amigável da fonte |
| `Url` | endereço utilizado na coleta |
| `Tipo` | RSS, Atom ou WebHtml |
| `Status` | Ativa ou Inativa |
| `PeriodicidadeMinutos` | intervalo planejado de coleta |
| `CriadoEm` | data do cadastro |
| `CriadoPorUsuarioId` | usuário responsável pelo cadastro |
| `AlteradoEm` | última alteração |
| `AlteradoPorUsuarioId` | quem fez a última alteração |

### Principais métodos

```csharp
Atualizar(...)
Ativar(...)
Desativar(...)
```

Esses métodos evitam que outras camadas alterem propriedades de qualquer jeito.

A entidade valida:

- nome;
- URL;
- periodicidade;
- usuário responsável.

### Exemplo

Isto não pode ser criado:

```csharp
new Fonte(
    "Portal",
    "url-invalida",
    TipoFonte.Rss,
    30,
    usuarioId);
```

A própria entidade lança `DomainException`.

## 3.3 Entidade ExecucaoColeta

Arquivo:

```text
src/InvoiSys.Domain/Entities/ExecucaoColeta.cs
```

Ela representa **uma execução específica** de coleta.

Não é a configuração da fonte.

É o registro de que "em determinado momento uma coleta foi iniciada".

### Propriedades importantes

| Propriedade | Significado |
|---|---|
| `FonteId` | fonte utilizada |
| `Inicio` | início da execução |
| `Fim` | fim da execução |
| `Status` | EmAndamento, Concluida ou Falhou |
| `TipoExecucao` | Manual ou Automatica |
| `QuantidadeDocumentos` | quantidade encontrada |
| `MensagemErro` | erro, se houver |
| `SolicitadaPorUsuarioId` | usuário que iniciou manualmente |

### Criação manual

```csharp
ExecucaoColeta.IniciarManual(fonteId, usuarioId);
```

Uma execução manual exige usuário.

### Criação automática

```csharp
ExecucaoColeta.IniciarAutomatica(fonteId);
```

Uma automática não possui usuário solicitante.

### Por que isso está no Domain

Porque não é regra de Controller nem de banco.

É uma regra do próprio negócio:

```text
manual → alguém pediu
automática → foi iniciada pelo sistema
```

## 3.4 Entidade Documento

Arquivo:

```text
src/InvoiSys.Domain/Entities/Documento.cs
```

Representa um item encontrado em uma coleta.

### Propriedades

| Propriedade | Significado |
|---|---|
| `ExecucaoColetaId` | execução que encontrou o documento |
| `Titulo` | título coletado |
| `UrlOriginal` | URL original |
| `Tipo` | tipo informado pelo coletor |
| `Origem` | fonte/origem |
| `DataPublicacao` | data da publicação, quando disponível |
| `DataColeta` | quando o InvoiSys coletou |
| `ConteudoTextual` | texto extraído |
| `Metadados` | JSON adicional |
| `Hash` | impressão digital do conteúdo |

O hash já é calculado e armazenado, mas **o sistema ainda não usa esse hash para change detection ou versionamento automático**.

## 3.5 Enums

### `StatusFonte`

```text
Ativa
Inativa
```

Uma fonte inativa não pode ser executada manualmente pelo Use Case atual.

### `TipoFonte`

```text
Rss
Atom
WebHtml
```

`WebHtml` existe no domínio, mas seu coletor ainda é futuro.

### `TipoExecucao`

```text
Manual
Automatica
```

### `StatusExecucaoColeta`

```text
EmAndamento
Concluida
Falhou
```

---

# 4. InvoiSys.Application

## 4.1 O que é

`InvoiSys.Application` contém os **casos de uso** do sistema.

Um Use Case representa uma intenção do usuário ou do sistema.

Exemplos:

- criar uma fonte;
- listar fontes;
- executar uma coleta;
- fazer login;
- criar usuário.

Ele coordena objetos e serviços, mas não deve implementar detalhes técnicos de PostgreSQL ou HTTP.

## 4.2 O que é um Use Case

Pense assim:

> Domain sabe as regras.  
> Infrastructure sabe os detalhes técnicos.  
> Application sabe a ordem das coisas.

Exemplo:

```text
"Cadastrar fonte"
```

O `CriarFonteUseCase` sabe que precisa:

1. validar o request;
2. descobrir o usuário atual;
3. criar a entidade;
4. mandar o repository adicionar;
5. mandar o Unit of Work confirmar;
6. devolver uma resposta.

## 4.3 Estrutura por funcionalidade

Exemplo:

```text
Fontes/
└── CriarFonte/
    ├── CriarFonteRequest.cs
    ├── CriarFonteValidator.cs
    ├── ICriarFonteUseCase.cs
    └── CriarFonteUseCase.cs
```

Esse formato ajuda porque tudo que pertence ao mesmo caso de uso fica próximo.

## 4.4 Request

O Request representa os dados que entram no caso de uso.

Exemplo conceitual:

```csharp
CriarFonteRequest
```

Ele carrega os valores necessários para criar uma fonte.

Request não é entidade.

Ele é apenas um contrato de entrada.

## 4.5 Validator

Exemplo:

```text
CriarFonteValidator
```

Usa FluentValidation para validar a entrada antes que o Use Case prossiga.

Ele cuida principalmente de validações de entrada.

A entidade ainda mantém suas próprias invariantes.

Isso significa que temos duas proteções com objetivos diferentes:

```text
Validator
→ dados recebidos estão adequados para o caso de uso?

Domain
→ a entidade pode existir neste estado?
```

## 4.6 CriarFonte

Fluxo:

```text
CriarFonteRequest
      ↓
CriarFonteValidator
      ↓
CriarFonteUseCase
      ↓
ICurrentUser
      ↓
Fonte
      ↓
IFonteRepository
      ↓
IUnitOfWork
```

Essa é uma das partes mais importantes para entender o projeto.

O Use Case depende de **interfaces**, não da implementação de banco.

## 4.7 Casos de uso de fontes existentes

A base atual possui:

```text
CriarFonte
ConsultarFonte
ListarFontes
AtualizarFonte
AlterarStatusFonte
```

`AlterarStatusFonteUseCase` é usado tanto para ativar quanto para desativar.

## 4.8 Casos de uso de coletas

Existem:

```text
ExecutarColetaManual
ConsultarColeta
ListarColetas
```

A coleta automática como rotina agendada **ainda não foi implementada**.

A entidade suporta execução automática, mas ainda não existe um worker/scheduler que a inicie periodicamente.

## 4.9 Casos de uso de documentos

```text
ConsultarDocumento
ListarDocumentos
```

## 4.10 Autenticação

```text
Login
Refresh
Logout
```

Os Use Cases conhecem abstrações de autenticação.

A implementação real fica em Infrastructure.

## 4.11 Usuários

```text
CriarUsuario
ListarUsuarios
AlterarStatusUsuario
RedefinirSenha
```

Apenas administrador acessa esses endpoints.

## 4.12 Interfaces de Repository

As interfaces atualmente estão no Domain:

```text
IFonteRepository
IExecucaoColetaRepository
IDocumentoRepository
IUnitOfWork
```

A Application usa essas abstrações sem conhecer EF Core.

## 4.13 ApiResponse

Arquivo:

```text
Application/Common/Responses/ApiResponse.cs
```

Estrutura real:

```csharp
public sealed record ApiResponse<T>(
    string Mensagem,
    T? Dados,
    bool Sucesso)
```

A versão atual **não possui uma propriedade separada chamada `Erros`**.

Quando existe erro de validação ou regra, a mensagem é preenchida no campo `Mensagem`.

Isso é importante para não documentar um contrato que não existe.

---

# 5. InvoiSys.Api

## 5.1 Responsabilidade

A API é a porta HTTP da aplicação.

Ela recebe:

```text
GET
POST
PUT
PATCH
```

e transforma isso em chamadas para a Application.

Ela também configura:

- Controllers;
- autenticação;
- autorização;
- CORS;
- Swagger;
- filtros;
- DI;
- pipeline HTTP.

## 5.2 Pastas principais

```text
InvoiSys.Api/
├── Configurations/
├── Controllers/
├── Filters/
├── Security/
├── Program.cs
└── appsettings*.json
```

## 5.3 Controllers

Controllers devem ser finos.

Exemplo real simplificado de `FontesController`:

```csharp
[ApiController]
[Route("api/fontes")]
[Authorize]
public sealed class FontesController(
    ICriarFonteUseCase criarFonteUseCase,
    IConsultarFonteUseCase consultarFonteUseCase,
    IListarFontesUseCase listarFontesUseCase,
    IAtualizarFonteUseCase atualizarFonteUseCase,
    IAlterarStatusFonteUseCase alterarStatusFonteUseCase)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarFonteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await criarFonteUseCase.ExecutarAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ConsultarPorId),
            new { id = response.Id },
            ApiResponse<FonteResponse>.Ok(
                "Fonte criada com sucesso.",
                response));
    }
}
```

### `[ApiController]`

Ativa comportamentos específicos de API no ASP.NET Core, como binding e validações de requisição.

### `[Route("api/fontes")]`

Define a rota base.

### `[HttpPost]`

Diz que aquele método responde a HTTP POST.

### `[Authorize]`

Exige usuário autenticado.

### `[Authorize(Roles = "...")]`

Além de autenticado, exige um dos perfis informados.

### `CancellationToken`

Permite cancelar uma operação quando a requisição é interrompida.

Isso evita continuar fazendo trabalho desnecessário.

### `async/await`

É usado porque banco, HTTP e outras operações I/O são assíncronas.

### `IActionResult`

Permite devolver diferentes respostas HTTP, como:

```text
200
201
400
404
```

## 5.4 O que não colocar em Controller

Um exemplo ruim seria:

```csharp
[HttpPost]
public async Task<IActionResult> Criar(...)
{
    var fonte = new Fonte(...);

    dbContext.Fontes.Add(fonte);
    await dbContext.SaveChangesAsync();

    // baixa RSS
    // processa XML
    // aplica regra de negócio

    return Ok();
}
```

Esse código mistura:

- HTTP;
- negócio;
- banco;
- integração externa.

No InvoiSys, cada parte vai para a camada adequada.

---

# 6. Padrão de resposta da API

A resposta de sucesso segue este formato:

```json
{
  "mensagem": "Fonte criada com sucesso.",
  "dados": {
    "id": "..."
  },
  "sucesso": true
}
```

Erro:

```json
{
  "mensagem": "Fonte não encontrada.",
  "dados": null,
  "sucesso": false
}
```

## 6.1 Status mais importantes

| Status | Significado no projeto |
|---|---|
| `200` | operação concluída |
| `201` | recurso criado |
| `400` | request/regra inválida |
| `401` | não autenticado ou sessão inválida |
| `403` | autenticado, mas sem perfil necessário |
| `404` | recurso não encontrado |
| `409` | conflito, quando aplicável |
| `500` | erro não tratado |

## 6.2 Como o frontend interpreta

Os clients retornam:

```csharp
ApiResponse<T>
```

A página pode verificar:

```csharp
response.Sucesso
response.Dados
response.Mensagem
```

No `AuthClient`, por exemplo:

```csharp
if (!response.Sucesso || response.Dados is null)
    return false;
```

---

# 7. Tratamento de erros

## 7.1 Ideia principal

Não usamos `try/catch` em todos os Controllers.

Existe um filtro global:

```text
ApiExceptionFilter
```

Ele transforma exceções em respostas HTTP padronizadas.

## 7.2 Exceções existentes

Na Application existem:

```text
InvoiSysException
BusinessException
ConflictException
NotFoundException
UnauthorizedException
```

No Domain existe:

```text
DomainException
```

Além disso, FluentValidation usa:

```text
ValidationException
```

## 7.3 Fluxo

```text
UseCase
  ↓
lança exceção
  ↓
ApiExceptionFilter
  ↓
converte em status HTTP
  ↓
ApiResponse
  ↓
Frontend
```

## 7.4 Erro desconhecido

Se acontecer uma exceção não prevista:

```csharp
logger.LogError(...)
```

e a API retorna status `500` com:

```text
"Erro desconhecido."
```

A exceção técnica completa fica no log, não é exposta diretamente ao usuário.

---

# 8. InvoiSys.Infrastructure

## 8.1 Responsabilidade

Infrastructure contém detalhes técnicos necessários para executar a aplicação.

Principalmente:

- PostgreSQL;
- EF Core;
- DbContext;
- repositories;
- Unit of Work;
- ASP.NET Core Identity;
- JWT;
- Refresh Token;
- implementação dos serviços de autenticação.

## 8.2 Por que banco não fica no Domain

O Domain deve responder:

> "Qual é a regra?"

Infrastructure responde:

> "Como vou salvar isso?"

Essas perguntas são diferentes.

`Fonte` não precisa saber se será salva em:

- PostgreSQL;
- SQL Server;
- memória;
- arquivo.

---

# 9. Entity Framework Core

## 9.1 O que é

EF Core é o ORM usado para conectar objetos C# ao banco relacional.

Ele permite trabalhar com entidades C# e traduz operações em SQL.

## 9.2 ApplicationDbContext

Arquivo:

```text
Infrastructure/Data/ApplicationDbContext.cs
```

Ele herda de:

```csharp
IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

Isso significa que o mesmo DbContext conhece:

- tabelas de negócio;
- tabelas do Identity.

DbSets atuais:

```csharp
DbSet<Fonte>
DbSet<ExecucaoColeta>
DbSet<Documento>
DbSet<RefreshToken>
```

## 9.3 EntityTypeConfiguration

Os mappings ficam separados.

Exemplos:

```text
FonteConfiguration
ExecucaoColetaConfiguration
DocumentoConfiguration
RefreshTokenConfiguration
```

Isso evita um `OnModelCreating` enorme.

### Exemplo

`FonteConfiguration` define:

- tabela `fontes`;
- chave;
- tamanho de campos;
- enum salvo como string;
- índices.

## 9.4 Fluxo de persistência

```text
Fonte
  ↓
FonteRepository
  ↓
ApplicationDbContext
  ↓
EF Core
  ↓
Npgsql
  ↓
PostgreSQL
```

## 9.5 Tracking

Quando EF Core busca uma entidade normalmente, ele pode acompanhar alterações.

No `FonteRepository`, listagens usam:

```csharp
AsNoTracking()
```

porque são somente leitura.

Isso reduz trabalho desnecessário.

Para uma entidade que será alterada, ela é buscada com tracking.

## 9.6 SaveChangesAsync

Repositories adicionam ou consultam objetos.

Quem confirma alterações é o `UnitOfWork`:

```csharp
dbContext.SaveChangesAsync(...)
```

## 9.7 Migrations

Migration é a descrição versionada das mudanças no banco.

No estado atual, a pasta de migration possui apenas um `README.md`: a `InitialCreate` ainda precisa ser gerada.

### Criar migration

Na raiz:

```powershell
dotnet ef migrations add InitialCreate `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api `
  --output-dir Migrations
```

### Atualizar o banco

```powershell
dotnet ef database update `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api
```

### Listar migrations

```powershell
dotnet ef migrations list `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api
```

---

# 10. PostgreSQL

## 10.1 Por que foi escolhido

PostgreSQL é um banco relacional maduro, gratuito e adequado para a estrutura do InvoiSys.

Também possui bom suporte para JSON.

No projeto, `Documento.Metadados` é mapeado para:

```text
jsonb
```

## 10.2 Connection string

A API procura:

```text
ConnectionStrings:invoisys
```

Exemplo por variável de ambiente:

```text
ConnectionStrings__invoisys
```

Senhas reais não devem entrar no `appsettings.json`.

## 10.3 Principais tabelas esperadas

```text
usuarios
perfis
usuarios_perfis
fontes
execucoes_coleta
documentos
refresh_tokens
```

Além das outras tabelas auxiliares do Identity.

## 10.4 Relacionamentos simplificados

```text
Usuario
 ├── cadastra/altera Fonte por IDs de auditoria
 ├── solicita ExecucaoColeta manual
 └── possui RefreshToken/Sessão

Fonte
 └── 1:N ExecucaoColeta

ExecucaoColeta
 └── 1:N Documento

Usuario
 └── 1:N RefreshToken
```

Os relacionamentos de auditoria da Fonte e da execução estão representados por IDs no Domain; nem todos estão configurados como navegações EF para `ApplicationUser`.

---

# 11. Repository

## 11.1 O que é

Repository esconde os detalhes de persistência da camada de aplicação.

Exemplo:

```csharp
IFonteRepository
```

A Application sabe que consegue:

- adicionar;
- obter por ID;
- listar.

Ela não precisa escrever:

```csharp
dbContext.Fontes.Where(...)
```

## 11.2 Interface e implementação

Interface:

```text
InvoiSys.Domain/Repositories/IFonteRepository.cs
```

Implementação:

```text
InvoiSys.Infrastructure/Repositories/FonteRepository.cs
```

Isso permite:

```text
Application → IFonteRepository
```

em vez de:

```text
Application → ApplicationDbContext
```

## 11.3 Exemplo de inclusão

```text
CriarFonteUseCase
  ↓
fonteRepository.AdicionarAsync(fonte)
  ↓
UnitOfWork.CommitAsync()
```

## 11.4 Exemplo de consulta

`FonteRepository.ListarAsync`:

- cria query;
- aplica filtros;
- conta total;
- ordena;
- pagina;
- retorna itens.

---

# 12. Unit of Work

## 12.1 O que é

Na base atual, `IUnitOfWork` é uma abstração simples para confirmar as alterações.

Implementação:

```csharp
public Task<int> CommitAsync(...) =>
    dbContext.SaveChangesAsync(...);
```

Uma forma simples de lembrar:

> Repository prepara a alteração.  
> Unit of Work confirma.

## 12.2 Exemplo

```text
Adicionar Fonte
   ↓
Repository.Add
   ↓
entidade está no DbContext
   ↓
UnitOfWork.Commit
   ↓
SaveChangesAsync
   ↓
PostgreSQL
```

## 12.3 Por que é útil

Em uma coleta manual temos várias alterações:

- adicionar execução;
- adicionar documentos;
- atualizar status da execução.

O Use Case pode coordenar tudo e confirmar de forma centralizada.

---

# 13. InvoiSys.Collectors

## 13.1 Por que existe separado

Coletar dados externos é uma responsabilidade específica do InvoiSys.

Não é responsabilidade da API.

Também não é responsabilidade da entidade.

Separar permite futuramente ter:

```text
RssAtomCollector
WebHtmlCollector
outros coletores
```

sem transformar Controller ou Use Case em uma grande sequência de `if/switch`.

## 13.2 IContentCollector

A abstração fica na Application:

```text
Application/Common/Collectors/IContentCollector.cs
```

Ela define o contrato que um coletor precisa cumprir.

## 13.3 CollectorResolver

O resolver recebe todos os coletores registrados e procura quem suporta determinado `TipoFonte`.

Fluxo:

```text
TipoFonte.Rss
  ↓
CollectorResolver
  ↓
RssAtomCollector.Suporta(...)
  ↓
coletor escolhido
```

Se nenhum suportar:

```text
BusinessException
```

## 13.4 RssAtomCollector

Arquivo:

```text
Collectors/RssAtom/RssAtomCollector.cs
```

Ele:

1. recebe URL;
2. usa `HttpClient`;
3. baixa XML;
4. carrega `XDocument`;
5. identifica RSS ou Atom;
6. percorre itens;
7. extrai dados;
8. remove HTML simples do conteúdo;
9. calcula hash SHA-256;
10. retorna `CollectedDocument`.

## 13.5 Fluxo real

```text
Fonte
  ↓
ExecutarColetaManualUseCase
  ↓
CollectorResolver
  ↓
RssAtomCollector
  ↓
HttpClient
  ↓
Fonte externa
  ↓
XML
  ↓
CollectedDocument
  ↓
Documento
```

---

# 14. RSS e Atom

## 14.1 RSS

RSS é um formato XML muito usado para publicar uma lista de conteúdos.

Exemplo conceitual:

```xml
<item>
  <title>Nova publicação</title>
  <link>https://exemplo.com/publicacao</link>
  <description>Conteúdo...</description>
  <pubDate>...</pubDate>
</item>
```

O InvoiSys lê cada `<item>`.

## 14.2 Atom

Atom tem objetivo parecido, mas outra estrutura.

Exemplo:

```xml
<entry>
  <title>Nova publicação</title>
  <link href="https://exemplo.com/publicacao" />
  <summary>Conteúdo...</summary>
  <updated>...</updated>
</entry>
```

O coletor procura elementos pelo `LocalName`, o que ajuda com namespaces XML.

## 14.3 Conversão em documento

Para RSS/Atom são extraídos dados como:

```text
Título
URL
Data de publicação
Conteúdo
Metadados
Origem
Hash
```

Depois a Application cria uma entidade `Documento`.

---

# 15. Autenticação

Esta parte merece atenção porque envolve vários conceitos diferentes.

## 15.1 Identity

ASP.NET Core Identity cuida de usuários e senhas.

No projeto existe:

```text
ApplicationUser
```

que herda de:

```csharp
IdentityUser<Guid>
```

Além dos campos do Identity, adicionamos:

```text
Nome
Ativo
CriadoEm
```

A senha não é guardada em texto puro.

Identity mantém um hash de senha.

## 15.2 JWT

JWT é o Access Token utilizado nas chamadas autenticadas.

Depois do login, a API devolve um token.

O frontend envia:

```http
Authorization: Bearer <token>
```

## 15.3 Access Token

Na configuração atual:

```text
30 minutos
```

Ele contém claims como:

```text
sub
NameIdentifier
email
name
role
sid
```

## 15.4 Claims

Claims são informações incluídas no token.

Exemplo:

```text
NameIdentifier → ID do usuário
Email → e-mail
Role → perfil
sid → ID da sessão
```

## 15.5 Refresh Token

O JWT é curto.

O Refresh Token permite renovar a sessão sem pedir senha novamente.

Configuração atual:

```text
7 dias
```

Ele é:

- aleatório;
- enviado em cookie HttpOnly;
- armazenado no banco apenas como hash.

## 15.6 Por que o Refresh Token não fica puro no banco

Imagine um vazamento da tabela.

Se o banco guardasse o token puro, alguém poderia usar aquele valor diretamente.

Guardando SHA-256:

```text
token real → navegador
hash do token → banco
```

a tabela sozinha não entrega o segredo original.

É o mesmo princípio de não armazenar senha em texto puro, embora o mecanismo de senha do Identity seja diferente e próprio para senhas.

## 15.7 Fluxo de login

```text
Tela Login
   ↓
AuthClient
   ↓
IAuthApi
   ↓
POST /api/auth/login
   ↓
AuthController
   ↓
LoginUseCase
   ↓
IAuthenticationService
   ↓
AuthenticationService
   ↓
UserManager
   ↓
valida e-mail + senha + usuário ativo
   ↓
JwtTokenService
   ├── cria JWT
   └── cria Refresh Token
   ↓
RefreshToken hash → banco
   ↓
JWT → corpo da resposta
Refresh Token → cookie HttpOnly
```

## 15.8 Session ID (`sid`)

Cada login cria:

```csharp
var sessaoId = Guid.NewGuid();
```

Esse ID entra:

- no JWT como claim `sid`;
- no registro do Refresh Token.

Quando o JWT chega à API, o evento `OnTokenValidated` verifica se:

- usuário existe/está ativo;
- a sessão existe;
- não foi revogada;
- não expirou.

Isso permite invalidar uma sessão antes do JWT chegar ao tempo normal de expiração.

## 15.9 Refresh

Fluxo:

```text
Frontend
  ↓
POST /api/auth/refresh
  ↓
browser envia cookie HttpOnly
  ↓
API calcula hash
  ↓
busca sessão
  ↓
valida
  ↓
gera novo Access Token
  ↓
rotaciona Refresh Token
```

## 15.10 Logout

```text
POST /api/auth/logout
```

A API:

1. lê cookie;
2. calcula hash;
3. encontra a sessão;
4. registra `RevogadoEm`;
5. remove cookie.

## 15.11 Usuário inativo

Quando um administrador inativa usuário:

```text
ApplicationUser.Ativo = false
```

e as sessões abertas são revogadas.

Além disso, o validador de sessão verifica `Usuario.Ativo`.

## 15.12 Redefinição de senha

Ao redefinir senha:

1. Identity gera token interno de reset;
2. senha é alterada;
3. sessões existentes são revogadas.

---

# 16. Autorização

Autenticação responde:

> Quem é você?

Autorização responde:

> Você pode fazer isso?

Perfis atuais:

```text
Administrador
Operador
Consulta
```

## 16.1 Administrador

Pode acessar gerenciamento de usuários e operações administrativas.

Exemplo real:

```csharp
[Authorize(Roles = "Administrador")]
public sealed class UsuariosController ...
```

## 16.2 Operador

Pode trabalhar com fontes e coletas.

Exemplo:

```csharp
[Authorize(Roles = "Administrador,Operador")]
```

## 16.3 Consulta

Pode entrar em endpoints que exigem somente:

```csharp
[Authorize]
```

mas não consegue executar os endpoints que exigem Admin/Operador.

---

# 17. InvoiSys.Web

## 17.1 O que é

É o frontend em Blazor WebAssembly.

O código .NET roda no navegador via WebAssembly e chama a API por HTTP.

## 17.2 Estrutura

```text
InvoiSys.Web/
├── Authentication/
├── Clients/
├── Components/
├── Contracts/
├── Layout/
├── Pages/
├── Theme/
├── wwwroot/
└── Program.cs
```

## 17.3 Pages

Páginas atuais:

```text
/login
/
/fontes
/coletas
/documentos
/usuarios
```

Estado real:

- Login: funcional como estrutura inicial;
- Dashboard: visual básico, sem métricas reais;
- Fontes: listagem básica;
- Coletas: listagem básica;
- Documentos: listagem básica;
- Usuários: listagem básica;
- formulários completos e experiência final ainda não estão prontos.

## 17.4 Clients

A interface não chama URLs espalhadas em cada página.

Existem interfaces Refit:

```text
IAuthApi
IFontesClient
IColetasClient
IDocumentosClient
IUsuariosClient
```

### Fluxo de Fontes

```text
Fontes.razor
  ↓
IFontesClient
  ↓
Refit/HttpClient
  ↓
GET /api/fontes
  ↓
API
  ↓
ApiResponse<PagedResponse<FonteResponse>>
  ↓
MudTable
```

## 17.5 Por que não chamar HttpClient direto em cada página

Se cada tela conhecer URL, headers, token e serialização:

- teremos código repetido;
- fica difícil padronizar erros;
- alterações na API afetam várias páginas.

O client concentra a comunicação.

## 17.6 AuthenticationStateProvider

`ApiAuthenticationStateProvider` converte a sessão atual em `ClaimsPrincipal`.

Isso permite Blazor entender:

```text
usuário autenticado?
qual nome?
qual role?
```

e usar:

```razor
@attribute [Authorize]
```

ou:

```razor
@attribute [Authorize(Roles = "Administrador")]
```

## 17.7 TokenStore

Na base atual o Access Token fica em memória, dentro de `TokenStore`.

Ele não é salvo em localStorage.

Ao recarregar a aplicação, o `AuthClient` tenta restaurar a sessão usando o Refresh Token HttpOnly.

---

# 18. MudBlazor

MudBlazor é a biblioteca de componentes visuais usada no frontend.

Ela evita criar do zero componentes básicos.

Na base atual já aparecem componentes como:

```text
MudButton
MudTable
MudTextField
MudAlert
MudPaper
MudGrid
MudText
MudProgressLinear
```

Exemplo:

```razor
<MudButton Variant="Variant.Filled"
           Color="Color.Primary">
    Entrar
</MudButton>
```

MudBlazor cuida da base visual, mas a regra da tela continua sendo nossa.

---

# 19. HttpClient

## 19.1 O que é

`HttpClient` é a API .NET usada para realizar requisições HTTP.

No InvoiSys ele aparece em dois contextos principais:

1. Collectors chamando fontes externas;
2. Web chamando a API.

## 19.2 AddHttpClient

Em `InvoiSys.Collectors`:

```csharp
services.AddHttpClient<RssAtomCollector>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("InvoiSys/1.0");
});
```

Isso registra um typed client.

O `RssAtomCollector` recebe um `HttpClient` configurado.

## 19.3 O problema de Microsoft.Extensions.Http

`InvoiSys.Collectors` é uma Class Library comum.

Por isso foi adicionada explicitamente a referência:

```text
Microsoft.Extensions.Http
```

Ela fornece as extensões necessárias para `AddHttpClient`.

Projetos ASP.NET Core que usam o shared framework podem receber várias APIs diretamente pelo framework.

Uma Class Library isolada não deve assumir automaticamente que possui a mesma referência.

## 19.4 Refit e HttpClient

No Web usamos:

```csharp
AddRefitClient<IFontesClient>()
```

Por baixo, Refit usa `HttpClient`.

## 19.5 Bearer Token

`AuthorizedHttpMessageHandler` adiciona:

```http
Authorization: Bearer <access-token>
```

antes de enviar as chamadas autenticadas.

---

# 20. Docker

## 20.1 Conceitos

### Imagem

É o pacote que descreve o ambiente da aplicação.

### Container

É uma instância executando uma imagem.

### Dockerfile

Receita para construir uma imagem.

### Docker Compose

Arquivo que descreve vários serviços que precisam rodar juntos.

### Volume

Armazenamento persistente fora do ciclo de vida do container.

### Environment variable

Configuração externa, como senha e connection string.

### Network

Comunicação interna entre containers.

## 20.2 Docker no InvoiSys

Arquivo:

```text
deploy/compose.yaml
```

Estrutura:

```text
Docker Compose
├── postgres
├── api
└── web
```

## 20.3 PostgreSQL

Imagem atual:

```text
postgres:18-alpine
```

O banco usa volume:

```text
postgres-data
```

Assim, remover/recriar o container não deve apagar automaticamente os dados do volume.

## 20.4 Subir

Na pasta `deploy`:

```powershell
Copy-Item .env.example .env
```

Edite as senhas.

Depois:

```powershell
docker compose up --build
```

## 20.5 Parar

```powershell
docker compose down
```

Para remover também volumes — cuidado, isso apaga dados persistidos:

```powershell
docker compose down -v
```

## 20.6 Por que senhas não ficam no código

No compose usamos:

```text
${POSTGRES_PASSWORD}
${JWT_KEY}
${BOOTSTRAP_ADMIN_PASSWORD}
```

Esses valores vêm do ambiente/.env local.

O `.env` real não deve ser commitado.

---

# 21. .NET Aspire

## 21.1 O que é

Aspire ajuda a executar e observar aplicações .NET formadas por vários serviços.

No InvoiSys temos:

```text
InvoiSys.AppHost
```

como projeto orquestrador.

## 21.2 AppHost

O AppHost descreve:

```text
AppHost
├── PostgreSQL
├── API
└── Web
```

Ele configura dependências e espera serviços necessários ficarem prontos.

## 21.3 Dashboard

Ao executar o AppHost, o Aspire disponibiliza um dashboard de desenvolvimento.

Ele ajuda a observar:

- recursos;
- logs;
- health;
- traces;
- métricas.

## 21.4 Service Discovery

Em vez de depender sempre de endereço fixo, os serviços podem descobrir os outros recursos registrados pelo Aspire.

## 21.5 Health Checks

A API expõe:

```text
/health
/alive
```

`/alive` serve como verificação simples de que o processo está vivo.

## 21.6 OpenTelemetry

OpenTelemetry é usado para observabilidade.

O ServiceDefaults configura:

- logs;
- métricas;
- traces.

## 21.7 Docker x Aspire

Eles não são a mesma coisa.

### Docker

Foco:

- empacotamento;
- containers;
- execução reproduzível;
- ambientes.

### Aspire

Foco:

- orquestração do ambiente .NET;
- experiência de desenvolvimento;
- service discovery;
- observabilidade;
- recursos distribuídos.

Eles podem ser usados juntos.

---

# 22. InvoiSys.ServiceDefaults

Esse projeto centraliza configurações que podem ser compartilhadas entre serviços.

Arquivo principal:

```text
ServiceDefaults/Extensions.cs
```

Ele configura:

```text
OpenTelemetry
Health Checks
Service Discovery
HTTP Resilience
```

## 22.1 Resiliência HTTP

```csharp
http.AddStandardResilienceHandler();
```

adiciona políticas padrão de resiliência em HttpClients configurados pelo host.

## 22.2 Vantagem

Sem ServiceDefaults, cada serviço teria que repetir:

```text
telemetria
health checks
service discovery
resiliência
```

---

# 23. Dependency Injection

## 23.1 O que é

DI permite que uma classe peça uma abstração sem criar manualmente a implementação.

Exemplo:

```text
FontesController precisa de ICriarFonteUseCase
```

O container sabe que:

```text
ICriarFonteUseCase → CriarFonteUseCase
```

Então ele cria e injeta.

## 23.2 Exemplo real

Em `Application.DependencyInjection`:

```csharp
services.AddScoped<ICriarFonteUseCase, CriarFonteUseCase>();
```

## 23.3 Lifetimes

### Transient

Nova instância a cada resolução.

No projeto, handlers HTTP auxiliares são registrados como transient.

### Scoped

Uma instância por escopo.

Em APIs, normalmente corresponde à requisição.

Use Cases, repositories e DbContext trabalham principalmente com scoped.

### Singleton

Uma instância durante toda a vida da aplicação.

No Web, `TokenStore` é singleton dentro da aplicação WASM.

---

# 24. Configurações

## 24.1 appsettings.json

Contém configuração não secreta e valores padrão.

Exemplo:

```text
Jwt:Issuer
Jwt:Audience
Jwt:AccessTokenMinutes
Jwt:RefreshTokenDays
```

A chave JWT não está no arquivo.

## 24.2 appsettings.Development.json

Configura detalhes de desenvolvimento, como origens CORS locais.

## 24.3 Environment variables

São adequadas para:

```text
ConnectionStrings__invoisys
Jwt__Key
BootstrapAdmin__Email
BootstrapAdmin__Password
```

## 24.4 user-secrets

No desenvolvimento local, podem guardar segredos sem gravá-los no repositório.

Exemplo:

```powershell
dotnet user-secrets set "Jwt:Key" "..."
```

Para usar isso, o projeto precisa ter suporte/ID de user-secrets configurado conforme o modo escolhido. Na base atual, o fluxo documentado prioriza variáveis de ambiente e parâmetros do Aspire.

## 24.5 Web appsettings

No Web:

```text
wwwroot/appsettings.json
```

contém:

```json
{
  "ApiBaseUrl": "https://localhost:7101/"
}
```

---

# 25. Program.cs

## 25.1 API

O `Program.cs` da API é curto porque a maior parte do registro foi dividida em extensões.

Fluxo:

```text
CreateBuilder
  ↓
AddServiceDefaults
  ↓
AddApiConfiguration
  ↓
AddAuthenticationConfiguration
  ↓
AddCorsConfiguration
  ↓
AddSwaggerConfiguration
  ↓
Build
  ↓
Development:
    Swagger
    Migrate
    Seed admin
  ↓
UseHttpsRedirection
UseCors
UseAuthentication
UseAuthorization
MapControllers
MapDefaultEndpoints
Run
```

### Observação importante

No ambiente Development, a API executa:

```csharp
dbContext.Database.MigrateAsync();
```

Por isso a migration inicial precisa existir antes da execução normal dessa base.

## 25.2 Web

O Web:

1. registra componentes raiz;
2. lê `ApiBaseUrl`;
3. adiciona MudBlazor;
4. configura autorização;
5. registra TokenStore;
6. configura Refit clients;
7. adiciona handlers;
8. tenta restaurar sessão;
9. executa.

## 25.3 AppHost

O AppHost:

1. define parâmetros secretos;
2. sobe PostgreSQL;
3. cria database `invoisys`;
4. registra API;
5. passa JWT/admin;
6. registra Web;
7. define dependências;
8. inicia aplicação distribuída.

---

# 26. Swagger / OpenAPI

Swagger fornece uma interface para visualizar e testar endpoints.

Na base atual ele é habilitado em Development.

Rota típica:

```text
/swagger
```

## 26.1 JWT no Swagger

Existe uma definição de segurança:

```text
Bearer
```

Depois de obter um JWT no endpoint de login, é possível autorizá-lo na interface Swagger e testar endpoints protegidos.

## 26.2 Anotações

Controllers usam atributos como:

```csharp
[SwaggerOperation(...)]
[SwaggerResponse(...)]
```

Isso melhora a descrição dos endpoints.

---

# 27. Testes

## 27.1 UnitTests

Testes unitários verificam partes pequenas sem precisar subir o sistema inteiro.

Existem testes reais para:

```text
Fonte
ExecucaoColeta
```

### Exemplo real

```text
CriarFonte_DeveFalhar_QuandoUrlForInvalida
```

Ele confirma que uma URL inválida gera `DomainException`.

Outro:

```text
IniciarAutomatica_NaoDevePossuirUsuarioSolicitante
```

confirma a regra da coleta automática.

## 27.2 IntegrationTests

O projeto de integração usa:

```text
Aspire.Hosting.Testing
```

Existe um smoke test que pretende:

1. subir o AppHost;
2. subir recursos;
3. esperar API saudável;
4. chamar `/alive`;
5. verificar HTTP 200.

No estado atual, esse teste está marcado com:

```csharp
Skip
```

porque depende:

- Docker;
- migration `InitialCreate`.

Portanto, não devemos afirmar que integração está validada neste momento.

## 27.3 xUnit

Framework de testes.

Versão atual no `Directory.Packages.props`:

```text
xunit.v3 4.0.0
```

## 27.4 FluentAssertions

Permite escrever asserts mais legíveis:

```csharp
fonte.Status.Should().Be(StatusFonte.Ativa);
```

---

# 28. Fluxos completos

## 28.1 Login

```text
Login.razor
  ↓
AuthClient.LoginAsync
  ↓
IAuthApi.LoginAsync
  ↓
POST /api/auth/login
  ↓
AuthController.LoginAsync
  ↓
LoginUseCase
  ↓
IAuthenticationService
  ↓
AuthenticationService
  ↓
UserManager
  ↓
JwtTokenService
  ├── JWT
  └── Refresh Token
  ↓
refresh hash → ApplicationDbContext
  ↓
PostgreSQL
  ↓
JWT → TokenStore
cookie HttpOnly → navegador
```

## 28.2 Cadastro de fonte

O endpoint e o Use Case existem.

A tela atual de Fontes ainda é basicamente de listagem; o formulário final de cadastro ainda precisa ser construído.

Fluxo de backend:

```text
POST /api/fontes
  ↓
FontesController.CriarAsync
  ↓
CriarFonteUseCase
  ↓
CriarFonteValidator
  ↓
ICurrentUser
  ↓
Fonte
  ↓
IFonteRepository
  ↓
IUnitOfWork
  ↓
PostgreSQL
```

## 28.3 Listagem de fontes

```text
Fontes.razor
  ↓
IFontesClient.ListarAsync
  ↓
GET /api/fontes
  ↓
FontesController.ListarAsync
  ↓
ListarFontesUseCase
  ↓
IFonteRepository.ListarAsync
  ↓
PostgreSQL
  ↓
PagedResponse<FonteResponse>
  ↓
MudTable
```

## 28.4 Coleta manual

```text
IColetasClient.ExecutarAsync
  ↓
POST /api/coletas/fontes/{fonteId}/executar
  ↓
ColetasController.ExecutarManualAsync
  ↓
ExecutarColetaManualUseCase
  ↓
IFonteRepository
  ↓
ExecucaoColeta.IniciarManual
  ↓
CollectorResolver
  ↓
RssAtomCollector
  ↓
HttpClient
  ↓
RSS/Atom externo
  ↓
CollectedDocument
  ↓
Documento
  ↓
IDocumentoRepository
  ↓
ExecucaoColeta.Concluir
  ↓
IUnitOfWork
  ↓
PostgreSQL
```

A interface atual ainda não apresenta um fluxo visual completo para disparar isso.

## 28.5 Coleta automática

Estado atual:

```text
Domain suporta TipoExecucao.Automatica
Domain suporta ExecucaoColeta.IniciarAutomatica
```

Porém:

```text
NÃO existe scheduler/worker implementado
```

Então o fluxo futuro esperado será parecido com:

```text
Scheduler/BackgroundService
  ↓
fontes ativas vencidas
  ↓
Use Case de coleta automática
  ↓
CollectorResolver
  ↓
Collector
  ↓
Banco
```

Esse fluxo é planejamento, não funcionalidade pronta.

## 28.6 Armazenamento de documento

```text
RssAtomCollector
  ↓
CollectedDocument
  ↓
ExecutarColetaManualUseCase
  ↓
new Documento(...)
  ↓
IDocumentoRepository.AdicionarVariosAsync
  ↓
IUnitOfWork.CommitAsync
  ↓
ApplicationDbContext
  ↓
documentos
```

## 28.7 Logout

```text
AuthClient.LogoutAsync
  ↓
IAuthApi.LogoutAsync
  ↓
POST /api/auth/logout
  ↓
AuthController
  ↓
LogoutUseCase
  ↓
AuthenticationService.LogoutAsync
  ↓
RefreshToken.RevogadoEm
  ↓
PostgreSQL
  ↓
cookie removido
  ↓
TokenStore.Limpar
```

---

# 29. Bibliotecas e pacotes realmente usados

A tabela abaixo considera referências presentes nos `.csproj` atuais.

| Biblioteca | Versão | Onde é usada | Para que serve |
|---|---:|---|---|
| Aspire.Hosting | 13.5.3 | AppHost | orquestra recursos |
| Aspire.Hosting.PostgreSQL | 13.5.3 | AppHost | recurso PostgreSQL no Aspire |
| Aspire.Hosting.Testing | 13.5.3 | IntegrationTests | testes com AppHost |
| FluentAssertions | 8.11.0 | testes | asserts legíveis |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Application | validators + DI |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.12 | Api, Infrastructure | autenticação JWT |
| Microsoft.AspNetCore.Components.Authorization | 10.0.12 | Web | autorização Blazor |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.12 | Web | frontend Blazor WASM |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.12 | Infrastructure | usuários/roles/Identity com EF |
| Microsoft.EntityFrameworkCore | 10.0.12 | Infrastructure | ORM |
| Microsoft.EntityFrameworkCore.Design | 10.0.12 | Infrastructure | tooling/migrations |
| Microsoft.Extensions.Http | 10.0.12 | Collectors | `AddHttpClient` |
| Microsoft.Extensions.Http.Resilience | 10.0.12 | ServiceDefaults | resiliência HTTP |
| Microsoft.Extensions.ServiceDiscovery | 10.0.12 | ServiceDefaults | service discovery |
| MudBlazor | 9.10.0 | Web | componentes visuais |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | Infrastructure | provider PostgreSQL |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.18.0 | ServiceDefaults | exportação OTLP |
| OpenTelemetry.Extensions.Hosting | 1.18.0 | ServiceDefaults | integração com host |
| OpenTelemetry.Instrumentation.AspNetCore | 1.18.0 | ServiceDefaults | telemetria HTTP server |
| OpenTelemetry.Instrumentation.Http | 1.18.0 | ServiceDefaults | telemetria HttpClient |
| OpenTelemetry.Instrumentation.Runtime | 1.18.0 | ServiceDefaults | métricas runtime |
| Refit | 15.2.0 | Web, IntegrationTests | clients HTTP por interface |
| Refit.HttpClientFactory | 15.2.0 | Web | integra Refit com HttpClientFactory |
| Swashbuckle.AspNetCore | 10.2.3 | Api | Swagger/OpenAPI |
| Swashbuckle.AspNetCore.Annotations | 10.2.3 | Api | anotações Swagger |
| Microsoft.NET.Test.Sdk | 18.8.1 | testes | infraestrutura de testes |
| xunit.v3 | 4.0.0 | testes | framework de testes |
| xunit.runner.visualstudio | 4.0.0 | testes | integração com runner/VS |
| coverlet.collector | 10.0.1 | testes | coleta de cobertura |

### O que não está sendo usado

`Testcontainers` não aparece nos `.csproj` atuais, então não faz parte desta documentação como biblioteca utilizada.

---

# 30. Estrutura de pastas

```text
InvoiSys/
├── src/
│   ├── InvoiSys.Api/
│   │   ├── Configurations/   # configuração HTTP, auth, CORS e Swagger
│   │   ├── Controllers/      # endpoints HTTP
│   │   ├── Filters/          # tratamento centralizado de exceções
│   │   ├── Security/         # usuário atual vindo do HttpContext
│   │   └── Program.cs        # composição e pipeline da API
│   │
│   ├── InvoiSys.Application/
│   │   ├── Autenticacao/     # login, refresh e logout
│   │   ├── Coletas/          # casos de uso das coletas
│   │   ├── Common/           # abstrações, respostas, exceptions, collectors
│   │   ├── Documentos/       # consulta/listagem
│   │   ├── Fontes/           # CRUD/status/listagem
│   │   ├── Usuarios/         # administração de usuários
│   │   └── DependencyInjection.cs
│   │
│   ├── InvoiSys.Domain/
│   │   ├── Entities/         # Fonte, ExecucaoColeta, Documento
│   │   ├── Enums/            # estados e tipos do negócio
│   │   ├── Exceptions/       # DomainException
│   │   └── Repositories/     # contratos de persistência
│   │
│   ├── InvoiSys.Infrastructure/
│   │   ├── Data/
│   │   │   ├── Configurations/ # mappings EF
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Identity/         # usuários, JWT, refresh, sessão
│   │   ├── Repositories/     # implementações EF
│   │   └── DependencyInjection.cs
│   │
│   ├── InvoiSys.Collectors/
│   │   ├── RssAtom/          # coletor implementado
│   │   ├── WebHtml/          # placeholder/futuro
│   │   ├── CollectorResolver.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── InvoiSys.Web/
│   │   ├── Authentication/   # sessão/token/handlers
│   │   ├── Clients/          # contratos Refit
│   │   ├── Components/       # componentes reutilizáveis
│   │   ├── Contracts/        # modelos específicos do Web
│   │   ├── Layout/           # layout e menu
│   │   ├── Pages/            # telas
│   │   ├── Theme/            # tema MudBlazor
│   │   └── wwwroot/          # arquivos públicos/config frontend
│   │
│   ├── InvoiSys.AppHost/     # orquestração Aspire
│   └── InvoiSys.ServiceDefaults/ # telemetria/health/discovery
│
├── tests/
│   ├── InvoiSys.UnitTests/
│   └── InvoiSys.IntegrationTests/
│
├── deploy/                   # Docker Compose
├── docs/                     # documentação
├── Directory.Packages.props  # versões centralizadas
├── Directory.Build.props     # padrões de build
├── global.json               # SDK .NET
└── InvoiSys.sln
```

---

# 31. Principais decisões arquiteturais

## PostgreSQL

Escolhido como banco relacional robusto e com suporte útil a `jsonb`.

## Collectors separados

A comunicação com RSS/Atom/Web é uma preocupação própria e deve poder crescer sem poluir API ou Domain.

## Identity + JWT

Identity resolve gestão segura de usuário/senha. JWT resolve autenticação das chamadas HTTP do frontend.

## Refresh Token

Evita depender de um JWT muito longo e permite renovação/revogação de sessão.

## Repository

Evita Application conhecer EF Core diretamente.

## Unit of Work

Centraliza a confirmação das alterações no DbContext.

## FluentValidation

Organiza validação de Request sem encher Use Cases de verificações de formato.

## ApiResponse

Padroniza o formato de resposta para o Web.

## Exception Filter

Evita `try/catch` repetido nos Controllers.

## Aspire

Facilita orquestração e observabilidade no desenvolvimento.

## Docker

Ajuda a executar ambiente reproduzível com PostgreSQL, API e Web.

---

# 32. O que está pronto e o que ainda falta

Esta seção é propositalmente clara para não dar impressão de sistema finalizado.

## 32.1 Já implementado na base

- estrutura de projetos/camadas;
- entidades `Fonte`, `ExecucaoColeta` e `Documento`;
- regras de domínio principais;
- CRUD básico/backend de fontes;
- listagem e consulta de coletas;
- listagem e consulta de documentos;
- coleta manual;
- `RssAtomCollector`;
- cálculo e armazenamento de hash;
- EF Core + PostgreSQL;
- mappings;
- repositories;
- Unit of Work;
- Identity;
- JWT;
- Refresh Token;
- roles;
- revogação de sessões;
- seed de administrador;
- Controllers;
- ApiResponse;
- Exception Filter;
- Swagger;
- Blazor base;
- autenticação no Blazor;
- Refit clients;
- MudBlazor;
- Aspire AppHost;
- ServiceDefaults;
- Dockerfiles/Compose;
- testes unitários de Domain.

## 32.2 Parcialmente implementado

### Frontend

Existem páginas e listagens básicas, mas não são telas finais.

Ainda faltam fluxos completos de:

- cadastro/edição de fonte na interface;
- ativação/desativação visual;
- execução manual pela interface;
- detalhes de coletas;
- detalhes de documentos;
- criação/edição de usuários pela interface;
- feedback de erros mais completo;
- paginação/filtros no frontend.

### Dashboard

Existe tela visual básica, mas ainda não busca indicadores reais.

### IntegrationTests

Existe estrutura e smoke test, porém o teste está `Skip`.

## 32.3 Ainda não implementado

- `WebHtmlCollector`;
- rotina automática/agendador de coletas;
- worker/background service;
- change detection efetivo usando hash;
- versionamento de documento;
- notificações;
- dashboard completo;
- métricas históricas de negócio;
- importação/exportação;
- monitoramento de disponibilidade das fontes;
- páginas finais conforme protótipo;
- migration `InitialCreate` gerada;
- validação final de build/test neste ambiente.

---

# 33. Como rodar o projeto no Windows

## 33.1 Requisitos

Na máquina de desenvolvimento:

```text
.NET SDK 10
Docker Desktop
Git
```

Para execução sem Docker, também é necessário PostgreSQL local.

O `global.json` atual pede:

```text
10.0.400
```

com:

```text
rollForward = latestFeature
```

## 33.2 Verificar SDK

```powershell
dotnet --list-sdks
dotnet --version
```

## 33.3 Restaurar ferramentas

```powershell
dotnet tool restore
```

## 33.4 Restaurar pacotes

```powershell
dotnet restore InvoiSys.sln
```

## 33.5 Build

```powershell
dotnet build InvoiSys.sln
```

## 33.6 Configurar banco/JWT para comandos EF

Exemplo temporário no PowerShell:

```powershell
$env:ConnectionStrings__invoisys="Host=localhost;Port=5432;Database=invoisys;Username=postgres;Password=SUA_SENHA"
$env:Jwt__Key="COLOQUE_AQUI_UMA_CHAVE_FORTE_COM_PELO_MENOS_64_CARACTERES"
```

## 33.7 Criar migration

```powershell
dotnet ef migrations add InitialCreate `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api `
  --output-dir Migrations
```

## 33.8 Aplicar migration

```powershell
dotnet ef database update `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api
```

## 33.9 Rodar API diretamente

Também configure admin inicial:

```powershell
$env:BootstrapAdmin__Email="admin@invoisys.local"
$env:BootstrapAdmin__Password="UmaSenhaForte123"
```

Depois:

```powershell
dotnet run --project src\InvoiSys.Api
```

## 33.10 Rodar Web

Em outro terminal:

```powershell
dotnet run --project src\InvoiSys.Web
```

## 33.11 Rodar via Aspire

O AppHost precisa dos parâmetros configurados.

A forma prática depende do mecanismo de secrets/parâmetros usado no ambiente local. Depois de configurados:

```powershell
dotnet run --project src\InvoiSys.AppHost
```

O Aspire exibirá o endereço do dashboard no terminal.

## 33.12 Rodar via Docker

```powershell
cd deploy
Copy-Item .env.example .env
```

Edite `.env`.

Depois:

```powershell
docker compose up --build
```

Parar:

```powershell
docker compose down
```

---

# 34. Problemas comuns

## SDK/global.json

### Sintoma

```text
Unable to resolve the .NET SDK version...
```

### Verificar

```powershell
dotnet --list-sdks
Get-Content .\global.json
```

O arquivo atual usa `10.0.400`.

---

## PostgreSQL não iniciado

### Sintoma

Erros de conexão como:

```text
connection refused
```

### Verificar Docker

```powershell
docker ps
```

ou serviço PostgreSQL local.

---

## Connection string ausente

A Infrastructure exige:

```text
ConnectionStrings:invoisys
```

Sem ela, a aplicação lança erro durante configuração.

---

## Migration não aplicada

Sintomas:

- tabela não existe;
- API falha no startup ao executar `MigrateAsync`.

Verifique:

```powershell
dotnet ef migrations list `
  --project src\InvoiSys.Infrastructure `
  --startup-project src\InvoiSys.Api
```

---

## JWT Key ausente ou curta

A API exige:

```text
Jwt:Key
```

com pelo menos 64 caracteres.

---

## Docker Desktop fechado

Comandos `docker compose` falham ao conectar no daemon.

Abra Docker Desktop e espere ficar pronto.

---

## Porta em uso

Pode aparecer:

```text
address already in use
```

Verifique a porta ou altere os valores no `.env`.

---

## Restore de pacote

Execute:

```powershell
dotnet restore InvoiSys.sln
```

Se houver erro de Central Package Management, confira se todo `PackageReference` possui `PackageVersion` correspondente no `Directory.Packages.props`.

---

## Erro 401

Possíveis causas:

- access token ausente;
- token expirou;
- sessão revogada;
- usuário inativo;
- `sid` inválido.

---

## Erro 403

O usuário está autenticado, mas não possui a Role necessária.

Exemplo:

```text
Consulta tentando criar Fonte.
```

---

# 35. Como eu explicaria o InvoiSys

## 35.1 Em 30 segundos

> O InvoiSys é uma plataforma para centralizar informações vindas de fontes relacionadas a DF-e. A gente cadastra uma fonte, como RSS ou Atom, executa uma coleta e o sistema guarda os documentos encontrados junto com origem, data, metadados e histórico da execução. O projeto foi separado em camadas para não misturar tela, regra de negócio, banco e integrações externas. Também tem autenticação com Identity, JWT e perfis de acesso.

## 35.2 Em 2 minutos

> O InvoiSys foi pensado para reduzir o trabalho manual de acompanhar várias fontes de informação. O usuário entra pelo frontend em Blazor, e as telas conversam com uma API ASP.NET Core. A API não concentra regra de negócio; ela chama a camada Application, onde ficam os casos de uso, como criar uma fonte ou executar uma coleta.
>
> O Domain é a parte mais central e protege regras que deveriam ser verdade independentemente da tecnologia. Por exemplo, uma coleta manual precisa registrar qual usuário solicitou, enquanto uma automática não tem usuário solicitante.
>
> O acesso ao banco fica na Infrastructure, usando Entity Framework Core e PostgreSQL. A parte de coleta ficou separada em Collectors. Hoje já existe um coletor RSS/Atom, e a ideia é adicionar Web/HTML depois sem precisar alterar os Controllers.
>
> Na autenticação usamos ASP.NET Core Identity para usuários e senhas, JWT para o access token e Refresh Token para renovar a sessão. Também temos Administrador, Operador e Consulta.
>
> Para desenvolvimento distribuído usamos .NET Aspire, e para executar os serviços em containers existe Docker Compose. A base ainda não está finalizada: a coleta automática, WebHtmlCollector e telas completas ainda serão desenvolvidas.

## 35.3 Explicação técnica em cerca de 5 minutos

> A solução é dividida em oito projetos principais. O `InvoiSys.Web` é Blazor WebAssembly e usa MudBlazor para os componentes. As chamadas HTTP ficam encapsuladas em clients Refit. Quando o usuário faz uma ação, o Web chama o `InvoiSys.Api`.
>
> A API tem Controllers finos. Eles recebem Request, chamam interfaces de Use Case e devolvem um `ApiResponse<T>`. Erros são tratados por um `ApiExceptionFilter`, então não precisamos repetir try/catch em todo Controller.
>
> A camada `Application` organiza os casos de uso. Por exemplo, na coleta manual, `ExecutarColetaManualUseCase` identifica o usuário atual, busca a fonte pelo repository, valida se está ativa, cria uma `ExecucaoColeta`, pede ao `CollectorResolver` o coletor adequado, transforma os itens coletados em `Documento`, salva e faz o commit.
>
> O `Domain` contém `Fonte`, `ExecucaoColeta` e `Documento`, além de enums e regras. Essas entidades não dependem de EF ou ASP.NET. A regra de uma execução manual exigir usuário está dentro de `ExecucaoColeta`, porque essa é uma regra de negócio.
>
> A `Infrastructure` implementa os repositories com EF Core, o `ApplicationDbContext`, PostgreSQL, Identity, JWT e Refresh Token. O Use Case conhece `IFonteRepository`; quem implementa de fato é `FonteRepository`.
>
> A autenticação começa no Identity, que valida email e senha. Depois é criado um JWT de 30 minutos e um Refresh Token de 7 dias. O Refresh Token fica em cookie HttpOnly e no banco guardamos apenas o hash. O JWT possui um `sid`, e a API valida a sessão em cada token, então inativar usuário ou revogar sessão invalida o acesso.
>
> `InvoiSys.Collectors` isola as integrações externas. Hoje o `RssAtomCollector` usa HttpClient, processa XML com XDocument e devolve objetos coletados. O tipo WebHtml já existe, mas ainda não foi implementado.
>
> O Aspire AppHost orquestra PostgreSQL, API e Web para desenvolvimento e o ServiceDefaults configura health checks, service discovery, resiliência e OpenTelemetry. Docker Compose é outra forma de subir PostgreSQL, API e Web em containers.
>
> O projeto ainda está em fase inicial: temos a arquitetura e vários fluxos backend, mas o frontend ainda é básico, a migration inicial precisa ser gerada, a coleta automática não existe e os testes de integração ainda estão desativados até preparar o ambiente.

---

# 36. Resumo para estudo

## Projetos

```text
InvoiSys.Api
→ recebe HTTP e chama casos de uso.

InvoiSys.Application
→ coordena o que precisa acontecer.

InvoiSys.Domain
→ contém entidades e regras de negócio.

InvoiSys.Infrastructure
→ banco, EF, Identity, JWT e repositories.

InvoiSys.Collectors
→ busca e transforma dados de fontes externas.

InvoiSys.Web
→ interface Blazor.

InvoiSys.AppHost
→ orquestra serviços com Aspire.

InvoiSys.ServiceDefaults
→ compartilha telemetria, health checks, discovery e resiliência.
```

## Conceitos

```text
JWT
→ token curto enviado no Authorization header.

Refresh Token
→ segredo de vida maior usado para renovar a sessão.

Repository
→ esconde detalhes de acesso ao banco.

Unit of Work
→ confirma as alterações no DbContext.

EF Core
→ mapeia objetos C# para PostgreSQL.

Docker
→ executa aplicações e banco em containers.

Aspire
→ orquestra e observa os serviços no desenvolvimento.

FluentValidation
→ valida Requests.

MudBlazor
→ componentes visuais para Blazor.

Refit
→ transforma interfaces C# em clients HTTP.

Identity
→ gerencia usuário, senha e roles.

OpenTelemetry
→ gera logs, traces e métricas observáveis.
```

---

# 37. Endpoints atuais

Esta tabela registra os endpoints encontrados nos Controllers atuais.

| Método | Rota | Autorização |
|---|---|---|
| POST | `/api/auth/login` | anônimo |
| POST | `/api/auth/refresh` | anônimo |
| POST | `/api/auth/logout` | anônimo |
| POST | `/api/fontes` | Administrador, Operador |
| GET | `/api/fontes/{id}` | autenticado |
| GET | `/api/fontes` | autenticado |
| PUT | `/api/fontes/{id}` | Administrador, Operador |
| PATCH | `/api/fontes/{id}/ativar` | Administrador, Operador |
| PATCH | `/api/fontes/{id}/desativar` | Administrador, Operador |
| POST | `/api/coletas/fontes/{fonteId}/executar` | Administrador, Operador |
| GET | `/api/coletas/{id}` | autenticado |
| GET | `/api/coletas` | autenticado |
| GET | `/api/documentos/{id}` | autenticado |
| GET | `/api/documentos` | autenticado |
| POST | `/api/usuarios` | Administrador |
| GET | `/api/usuarios` | Administrador |
| PATCH | `/api/usuarios/{id}/status` | Administrador |
| PUT | `/api/usuarios/{id}/senha` | Administrador |

Também existem:

```text
/health
/alive
```

por `ServiceDefaults`.

---

# 38. Checklist mental: onde colocar um código novo?

Antes de criar uma classe, pergunte:

### É regra da entidade?

```text
Domain
```

Exemplo:

> uma fonte precisa de URL válida.

### É uma ação do sistema?

```text
Application
```

Exemplo:

> cadastrar fonte.

### É banco/EF/Identity/JWT?

```text
Infrastructure
```

### É endpoint HTTP?

```text
Api
```

### É comunicação com portal/feed?

```text
Collectors
```

### É tela/componente/client do navegador?

```text
Web
```

### É observabilidade padrão compartilhada?

```text
ServiceDefaults
```

### É composição do ambiente distribuído?

```text
AppHost
```

---

# 39. Estado de validação desta documentação

Esta documentação foi montada a partir dos arquivos atuais da base e considera:

- nomes reais das classes presentes;
- endpoints atuais dos Controllers;
- packages realmente referenciados nos `.csproj`;
- estrutura real de DI;
- fluxo real do `ExecutarColetaManualUseCase`;
- autenticação real com Identity/JWT/Refresh Token;
- páginas atuais do Web;
- mappings atuais do EF;
- testes atuais.

Pontos que **não foram descritos como prontos** porque ainda não estão implementados:

```text
WebHtmlCollector
agendamento automático
dashboard real
change detection funcional
notificações
migration InitialCreate gerada
frontend final
testes de integração ativos
```

## Próximo passo técnico antes do ZIP final

A documentação está pronta, mas o ZIP final só deve ser considerado validado depois de:

```powershell
dotnet restore InvoiSys.sln
dotnet build InvoiSys.sln
dotnet test tests\InvoiSys.UnitTests\InvoiSys.UnitTests.csproj
dotnet test tests\InvoiSys.IntegrationTests\InvoiSys.IntegrationTests.csproj
```

Antes do último comando, é necessário:

- gerar a migration `InitialCreate`;
- ter Docker disponível;
- remover o `Skip` do smoke test quando o ambiente estiver preparado.

Se build ou teste exigir alteração no código, esta documentação deve ser atualizada junto com a correção.
