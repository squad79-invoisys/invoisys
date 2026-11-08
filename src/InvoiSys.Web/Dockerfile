FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/InvoiSys.Web/InvoiSys.Web.csproj", "src/InvoiSys.Web/"]
COPY ["src/InvoiSys.Application/InvoiSys.Application.csproj", "src/InvoiSys.Application/"]
COPY ["src/InvoiSys.Domain/InvoiSys.Domain.csproj", "src/InvoiSys.Domain/"]

RUN dotnet restore "src/InvoiSys.Web/InvoiSys.Web.csproj"

COPY . .

# O appsettings.json é reescrito quando o container sobe (veja
# docker-entrypoint.sh). As cópias comprimidas geradas no publish ficariam
# com o valor antigo, então saem da imagem.
RUN dotnet publish "src/InvoiSys.Web/InvoiSys.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false \
    && rm -f /app/publish/wwwroot/appsettings*.json.br \
             /app/publish/wwwroot/appsettings*.json.gz

FROM nginx:1.29-alpine AS final

# Endereço da API visto pelo navegador. "/" significa a mesma origem da tela,
# que é o caso quando um proxy (Caddy) encaminha /api/* para a API. Para a API
# em outra origem, passe a URL absoluta, por exemplo http://localhost:8080/.
ENV API_BASE_URL=/

COPY src/InvoiSys.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --chmod=755 src/InvoiSys.Web/docker-entrypoint.sh /docker-entrypoint.d/40-invoisys-config.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q --spider http://127.0.0.1/ || exit 1
