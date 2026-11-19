#!/bin/sh
# Executado pelo entrypoint oficial do nginx antes de iniciar o servidor.
# Grava o endereço da API no appsettings.json lido pelo Blazor no navegador,
# para que a mesma imagem sirva qualquer ambiente.
set -eu

API_BASE_URL="${API_BASE_URL:-/}"

case "$API_BASE_URL" in
  */) ;;
  *) API_BASE_URL="$API_BASE_URL/" ;;
esac

printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" \
  > /usr/share/nginx/html/appsettings.json

echo "InvoiSys Web: ApiBaseUrl=$API_BASE_URL"
