# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
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

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LawConnect.Api.dll"]
