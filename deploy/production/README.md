# Deploy do InvoiSys

Este documento tem duas partes: **o que já está pronto** no repositório e
**o que ainda precisa ser feito** no servidor e no GitHub.

---

## Situação atual

| Etapa | Status |
|---|---|
| Imagens Docker da API e da Web | **Pronto** |
| Publicação automática no GHCR a cada push na `main` | **Pronto** |
| Arquivos de produção (compose, proxy, HTTPS, script) | **Pronto** |
| Workflow de deploy por SSH | **Pronto, aguardando os secrets** |
| Migrations e admin inicial rodando em produção | **Pronto** |
| VPS contratado e configurado | **Falta** |
| Domínio apontado | **Falta** |
| Secrets e variables no GitHub | **Falta** |

Hoje, a cada push na `main`, as imagens são reconstruídas e publicadas
sozinhas. O que ainda não existe é o servidor que consome essas imagens.

> **Atenção à ordem:** enquanto a variable `API_BASE_URL` não for criada, a
> imagem Web é construída apontando para `http://localhost:8080`. Crie a
> variable **antes** de usar a imagem em produção.

---

## Como fica no servidor

Quatro containers numa rede interna, com o Caddy sendo a única coisa exposta
para a internet.

```
internet --> caddy (80/443, HTTPS automático)
               |-- /api/*  --> api  (ASP.NET, porta 8080, sem porta pública)
               |-- /health --> api
               \-- /*      --> web  (nginx servindo o Blazor WASM)

                             api --> postgres (sem porta pública, com volume)
```

A tela e a API ficam na **mesma origem**. Isso elimina o CORS e faz o cookie do
refresh token funcionar sem configuração extra.

---

## O que já está pronto

### Imagens publicadas automaticamente

O workflow `.github/workflows/docker-publish.yml` roda a cada push na `main`:
compila, executa os testes unitários e publica duas imagens no GHCR:

- `ghcr.io/squad79-invoisys/invoisys-api:latest`
- `ghcr.io/squad79-invoisys/invoisys-web:latest`

### Arquivos de produção

| Arquivo | Para que serve |
|---|---|
| `compose.yaml` | Sobe os quatro containers a partir das imagens do GHCR |
| `Caddyfile` | Proxy reverso, HTTPS automático e cabeçalhos de segurança |
| `.env.example` | Modelo das variáveis, com o comando para gerar cada segredo |
| `update.sh` | Script que atualiza os containers no servidor |

### Workflow de deploy

O `.github/workflows/deploy.yml` dispara **depois** da publicação das imagens,
conecta por SSH, troca os containers e só passa se `/health` responder 200.

Enquanto o secret `VPS_HOST` não existir, ele termina verde avisando que o
deploy foi ignorado — não fica falhando a cada push.

### Ajustes no código da API

Duas mudanças no `src/InvoiSys.Api/Program.cs` foram necessárias:

- **Migrations e admin inicial em produção.** Antes rodavam só em
  `Development`, então o banco de produção nasceria vazio e ninguém
  conseguiria entrar. Agora rodam também quando
  `Database__MigrateOnStartup=true`, que é o valor usado no compose.
- **`UseForwardedHeaders`.** Atrás de um proxy a API só recebe HTTP, e o código
  usa `Secure = Request.IsHttps` no cookie do refresh token. Sem isso o cookie
  sairia sem a flag `Secure` mesmo com HTTPS na frente.

---

## O que falta fazer

### 1. Preparar o VPS

No hPanel da Hostinger, abra o VPS e troque o sistema operacional para um
template **Ubuntu com Docker** já instalado. Guarde a senha de root.

Em **Firewall**, libere as portas **22**, **80** e **443**. Não libere a 5432
nem a 8080: o banco e a API não devem ser alcançáveis de fora.

```bash
ssh root@SEU_IP
```

```bash
docker --version && docker compose version
```

### 2. Apontar o domínio

No painel de DNS, crie os registros **A** apontando para o IP do VPS:

| Tipo | Nome | Valor |
|------|------|-------|
| A    | @    | IP do VPS |
| A    | www  | IP do VPS |

Espere propagar antes de subir, senão o Let's Encrypt falha ao emitir o
certificado:

```bash
dig +short SEU_DOMINIO
```

### 3. Colocar os arquivos no servidor

```bash
mkdir -p /opt/invoisys && cd /opt/invoisys && curl -fsSLO https://raw.githubusercontent.com/squad79-invoisys/invoisys/main/deploy/production/compose.yaml && curl -fsSLO https://raw.githubusercontent.com/squad79-invoisys/invoisys/main/deploy/production/Caddyfile && curl -fsSLO https://raw.githubusercontent.com/squad79-invoisys/invoisys/main/deploy/production/update.sh && chmod +x update.sh
```

### 4. Criar o `.env`

Use o `.env.example` como base e gere os segredos no próprio servidor:

```bash
openssl rand -base64 64 | tr -d '\n'
```

```bash
openssl rand -base64 24
```

O primeiro vira o `JWT_KEY`, o segundo a `POSTGRES_PASSWORD`. O arquivo guarda
a senha do banco, então proteja:

```bash
chmod 600 /opt/invoisys/.env
```

### 5. Subir

```bash
cd /opt/invoisys && docker compose up -d
```

```bash
cd /opt/invoisys && docker compose logs -f api
```

No primeiro start a API aplica as migrations e cria o administrador do `.env`.
O Caddy emite o certificado sozinho na primeira requisição ao domínio.

```bash
curl -i https://SEU_DOMINIO/health
```

### 6. Criar a chave SSH do deploy

Na **sua máquina**, não no servidor:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/invoisys_deploy -C "github-actions-deploy" -N ""
```

```bash
ssh-copy-id -i ~/.ssh/invoisys_deploy.pub root@SEU_IP
```

```bash
ssh-keyscan -H SEU_IP
```

### 7. Criar os secrets e variables no GitHub

Em **Settings > Secrets and variables > Actions**.

Secrets:

| Nome | Valor |
|------|-------|
| `VPS_HOST` | IP do VPS |
| `VPS_USER` | `root` |
| `VPS_SSH_KEY` | conteúdo de `~/.ssh/invoisys_deploy` (a chave **privada**, inteira) |
| `VPS_KNOWN_HOSTS` | saída do `ssh-keyscan -H SEU_IP` |
| `VPS_PORT` | opcional, só se a porta SSH não for 22 |

Variables:

| Nome | Valor |
|------|-------|
| `APP_DOMAIN` | `seudominio.com.br` |
| `API_BASE_URL` | `https://seudominio.com.br/api/` |

A `API_BASE_URL` é gravada **dentro da imagem Web durante o build**, porque o
Blazor WebAssembly roda no navegador e precisa saber o endereço da API antes de
ser servido. Depois de criar a variable, rode o publish de novo para gerar uma
imagem utilizável.

### 8. Deixar as imagens acessíveis

Em **Packages**, marque `invoisys-api` e `invoisys-web` como **public**. Para
mantê-las privadas, gere um PAT com `read:packages` e faça login no VPS uma vez:

```bash
echo SEU_PAT | docker login ghcr.io -u SEU_USUARIO --password-stdin
```

### 9. Testar

Rode o workflow "Deploy no VPS" pela aba Actions. A partir daí, todo merge na
`main` atualiza o site sozinho.

---

## Operação do dia a dia

```bash
cd /opt/invoisys && docker compose ps
```

```bash
cd /opt/invoisys && docker compose logs -f api
```

```bash
cd /opt/invoisys && ./update.sh
```

```bash
cd /opt/invoisys && docker compose restart api
```

Backup do banco:

```bash
cd /opt/invoisys && docker compose exec -T postgres pg_dump -U postgres invoisys | gzip > ~/invoisys-$(date +%F).sql.gz
```

---

## Quando algo dá errado

**O certificado não sai.** Quase sempre é DNS ainda não propagado ou porta 443
fechada no firewall. `docker compose logs caddy` mostra o erro do ACME.

**A API reinicia em loop.** Veja `docker compose logs api`. Se for conexão com
o banco, confira se a `POSTGRES_PASSWORD` do `.env` bate com o volume
existente: trocar a senha depois que o volume foi criado não muda a senha do
Postgres.

**A tela abre mas não loga.** Provavelmente a imagem Web foi construída sem a
`API_BASE_URL` certa. Abra o console do navegador: se as chamadas vão para
`localhost:8080`, é isso. Corrija a variable e rode o publish de novo.

**O deploy termina verde mas nada muda.** Veja se o passo "Verificar se o
servidor já foi configurado" avisou que o `VPS_HOST` não existe.
