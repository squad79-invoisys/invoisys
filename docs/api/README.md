# Documentação pública da API

Esta pasta publica a especificação OpenAPI da API como página estática, para que
o cliente consulte os endpoints sem precisar subir o projeto.

- `index.html` — a página, que carrega o Swagger UI por CDN
- `openapi.json` — a especificação, gerada a partir da API

A página é **somente leitura**: o botão de executar requisições fica desativado,
porque não há servidor por trás dela.

## Gerar o `openapi.json`

A API exige conexão com o banco já na inicialização, então **não existe forma de
gerar a especificação sem um PostgreSQL no ar**. Deixe o Docker Desktop ativo
antes de começar.

Suba o ambiente:

```bash
dotnet run --project src/InvoiSys.AppHost
```

Descubra a porta da API no painel do Aspire e baixe a especificação:

```bash
curl http://localhost:<porta-da-api>/swagger/v1/swagger.json -o docs/api/openapi.json
```

> O Swagger só é registrado quando o ambiente é `Development`. Em outros
> ambientes a rota não existe, por decisão no `Program.cs`.

## Publicar

Em `Settings → Pages`, defina:

- **Source:** Deploy from a branch
- **Branch:** `main`, pasta `/docs`

O endereço fica:

```
https://squad79-invoisys.github.io/invoisys/api/
```

> Se o repositório for privado, o GitHub Pages exige plano pago.

## Manter atualizado

A especificação não se atualiza sozinha. Sempre que um endpoint for criado,
removido ou tiver o contrato alterado, gere o arquivo de novo e faça commit.

Vale rodar isso antes de cada entrega ao cliente, para o link não mostrar uma
API diferente da que está no código.
