# LawConnect

Mobile-first marketplace connecting customers with verified lawyers.

## Solution structure

```
LawConnect/
├── src/
│   ├── LawConnect.Api
│   ├── LawConnect.Application
│   ├── LawConnect.Domain
│   ├── LawConnect.Infrastructure
│   └── LawConnect.Shared
├── tests/
│   └── LawConnect.Tests
├── docs/
├── docker-compose.yml
└── Dockerfile
```

## Prerequisites

- .NET 10 SDK
- Docker Desktop (PostgreSQL + Redis)

## Quick start

```bash
# Start infrastructure
docker compose up -d postgres redis

# Run API
dotnet run --project src/LawConnect.Api

# Run tests
dotnet test
```

Swagger (Development): http://localhost:5080/swagger

Health: http://localhost:5080/health

## Notes

- Target framework is `net10.0` (SDK available in this environment). Stack docs reference ASP.NET Core 9; align when a net9 targeting pack is installed.
- Coding standards: see `docs/14-Coding-Standards.md` (no mocks/placeholders, XML docs on public methods, zero-warning builds).
- Step 1: solution scaffolding, DI, logging, PostgreSQL, Redis, Hangfire, JWT wiring, Docker.
- Step 2: database foundation, core infrastructure, migration + seed, ER docs.
