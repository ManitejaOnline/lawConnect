# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY global.json ./
COPY .editorconfig ./
COPY LawConnect.sln ./
COPY src/LawConnect.Domain/LawConnect.Domain.csproj src/LawConnect.Domain/
COPY src/LawConnect.Shared/LawConnect.Shared.csproj src/LawConnect.Shared/
COPY src/LawConnect.Application/LawConnect.Application.csproj src/LawConnect.Application/
COPY src/LawConnect.Infrastructure/LawConnect.Infrastructure.csproj src/LawConnect.Infrastructure/
COPY src/LawConnect.Api/LawConnect.Api.csproj src/LawConnect.Api/

RUN dotnet restore src/LawConnect.Api/LawConnect.Api.csproj

COPY src/ ./src/

RUN dotnet publish src/LawConnect.Api/LawConnect.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Official .NET 8+ / 10 images define non-root APP_UID=1654 (user "app").
# The aspnet:10.0 image has no adduser; do not apt-install it.
COPY --chown=$APP_UID:$APP_UID --from=build /app/publish .

RUN mkdir -p /app/storage /app/logs \
    && chown -R $APP_UID:$APP_UID /app/storage /app/logs

USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_HTTP_PORTS=

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}; unset ASPNETCORE_HTTP_PORTS; exec dotnet LawConnect.Api.dll"]
