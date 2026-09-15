FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/InvoiSys.Web/InvoiSys.Web.csproj", "src/InvoiSys.Web/"]
COPY ["src/InvoiSys.Application/InvoiSys.Application.csproj", "src/InvoiSys.Application/"]
COPY ["src/InvoiSys.Domain/InvoiSys.Domain.csproj", "src/InvoiSys.Domain/"]

RUN dotnet restore "src/InvoiSys.Web/InvoiSys.Web.csproj"

COPY . .
ARG API_BASE_URL=http://localhost:8080/

RUN dotnet publish "src/InvoiSys.Web/InvoiSys.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false \
    && sed -i "s#https://localhost:7101/#${API_BASE_URL}#g" \
       /app/publish/wwwroot/appsettings.json

FROM nginx:1.29-alpine AS final

COPY src/InvoiSys.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
ENTRYPOINT ["nginx", "-g", "daemon off;"]
