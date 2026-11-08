#!/usr/bin/env bash
# Atualiza o InvoiSys para as imagens mais recentes publicadas no GHCR.
# Roda no VPS, dentro de /opt/invoisys.
set -euo pipefail

cd "$(dirname "$0")"

echo "==> Baixando imagens novas"
docker compose pull

echo "==> Subindo containers"
docker compose up -d --remove-orphans

echo "==> Limpando imagens antigas"
docker image prune -f

echo "==> Estado atual"
docker compose ps
