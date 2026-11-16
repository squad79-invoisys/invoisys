# Validação do índice único de documentos

## Resultado

O índice único B-tree sobre `("UrlOriginal", "Hash")` foi validado em PostgreSQL 18.3 com codificação UTF-8.

- URL ASCII com exatamente 2048 caracteres e hash com 64 caracteres: inserção bem-sucedida.
- URL multibyte variada com 2000 caracteres e hash com 64 caracteres: inserção rejeitada.

O PostgreSQL retornou:

```text
SQLSTATE 54000
index row size 6040 exceeds btree version 4 maximum 2704
Values larger than 1/3 of a buffer page cannot be indexed.
```

## Impacto

O modelo permite `UrlOriginal` com até 2048 caracteres, mas nem todo valor multibyte próximo desse limite pode ser representado no índice B-tree composto. URLs ASCII no limite foram aceitas.

Conforme a decisão técnica da task, a implementação não substitui automaticamente a chave por hash auxiliar, índice de expressão ou outra estratégia. O teste multibyte permanece documentado e ignorado com motivo explícito até revisão da modelagem ou do limite aceito para URL.
