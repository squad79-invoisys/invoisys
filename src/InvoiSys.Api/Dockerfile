FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/InvoiSys.Api/InvoiSys.Api.csproj", "src/InvoiSys.Api/"]
COPY ["src/InvoiSys.Application/InvoiSys.Application.csproj", "src/InvoiSys.Application/"]
COPY ["src/InvoiSys.Domain/InvoiSys.Domain.csproj", "src/InvoiSys.Domain/"]
COPY ["src/InvoiSys.Infrastructure/InvoiSys.Infrastructure.csproj", "src/InvoiSys.Infrastructure/"]
COPY ["src/InvoiSys.Collectors/InvoiSys.Collectors.csproj", "src/InvoiSys.Collectors/"]
COPY ["src/InvoiSys.ServiceDefaults/InvoiSys.ServiceDefaults.csproj", "src/InvoiSys.ServiceDefaults/"]

RUN dotnet restore "src/InvoiSys.Api/InvoiSys.Api.csproj"

COPY . .

RUN dotnet publish "src/InvoiSys.Api/InvoiSys.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "InvoiSys.Api.dll"]
