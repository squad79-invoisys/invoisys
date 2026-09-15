# Modelo de dados inicial

## Fonte

Representa uma origem monitorada.

Principais campos: nome, URL, tipo, status, periodicidade, criação e auditoria de alteração.

Tipos iniciais: `Rss`, `Atom`, `WebHtml`.

## ExecucaoColeta

Representa uma execução sobre uma Fonte.

- `Manual`: deve ter `SolicitadaPorUsuarioId`.
- `Automatica`: não possui usuário solicitante.
- Estados iniciais: `EmAndamento`, `Concluida`, `Falhou`.

Relação: uma Fonte possui várias execuções.

## Documento

Representa o conteúdo encontrado em uma execução.

São preservados:

- título;
- URL original;
- origem e tipo;
- data de publicação, quando disponível;
- data de coleta;
- conteúdo textual, quando possível;
- metadados em JSON (`jsonb` no PostgreSQL);
- hash para detectar mudanças de conteúdo futuramente.

Relação: uma execução pode gerar vários documentos.

## Identidade

O ASP.NET Core Identity mantém usuários, roles, claims e dados de autenticação. A tabela de refresh tokens mantém a sessão e a revogação sem armazenar o token bruto.
