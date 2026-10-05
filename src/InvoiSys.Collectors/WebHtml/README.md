# Web/HTML collector

Coleta o conteúdo de páginas web servidas em HTML, cobrindo o tipo `TipoFonte.WebHtml`.

Segue a mesma abstração `IContentCollector` usada por RSS/Atom, sem adicionar
lógica de coleta aos Controllers.

## Como funciona

1. Baixa a página e deixa o AngleSharp detectar a codificação pelo `meta charset`.
2. Lê o título em `<title>` e, se não houver, no primeiro `<h1>`.
3. Descarta os elementos sem conteúdo: `script`, `style`, `nav`, `footer`,
   `noscript`, `svg`, `iframe` e `template`.
4. Extrai o texto do primeiro elemento encontrado entre `<article>`, `<main>` e
   `<body>`, nessa ordem.
5. Calcula o hash do conteúdo com SHA-256, no mesmo formato do coletor de RSS, o
   que permite detectar alteração entre coletas.

Cada página coletada gera **um** documento.

## Decisões

**Uma página, um documento.** Páginas extensas, como o Manual de Orientação ao
Contribuinte, viram um único registro. Para recortar um trecho menor, use o
seletor por fonte previsto na KAN-32.

**Sem navegador headless.** As origens monitoradas hoje entregam HTML pronto no
servidor. Caso apareça uma fonte que monte o conteúdo por JavaScript, o AngleSharp
não resolve e será necessário avaliar outra estratégia.
