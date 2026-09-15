# Autenticação e autorização

## Fluxo

A autenticação usa ASP.NET Core Identity, JWT e Refresh Token.

1. Login com e-mail e senha.
2. A API valida o usuário no Identity.
3. É criada uma sessão com identificador próprio (`sid`).
4. A API devolve um JWT de curta duração.
5. O Refresh Token fica em cookie `HttpOnly` e apenas seu hash é armazenado no PostgreSQL.
6. Ao renovar, o refresh token é rotacionado.
7. Logout, inativação do usuário e redefinição de senha revogam sessões.

## Perfis

- `Administrador`: usuários, fontes, coletas, documentos e configurações.
- `Operador`: fontes, coletas e documentos.
- `Consulta`: leitura.

## Duração

- Access Token: 30 minutos.
- Refresh Token: 7 dias.

## Segurança

- Não existe cadastro público.
- Senhas são tratadas pelo ASP.NET Core Identity; senha em texto puro não é persistida.
- O valor bruto do refresh token não é persistido.
- A chave JWT deve vir de variável de ambiente/user-secrets e ter pelo menos 64 caracteres.
- Cada JWT contém `sid`; a API valida se a sessão continua ativa em `OnTokenValidated`.
