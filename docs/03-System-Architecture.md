# 03 System Architecture

## Style

Clean Architecture with CQRS (MediatR).

```
LawConnect.Api
    → LawConnect.Application
    → LawConnect.Domain
LawConnect.Api
    → LawConnect.Infrastructure
        → LawConnect.Application
        → LawConnect.Domain
LawConnect.Shared ← referenced by Application / Infrastructure / Api
```

## Cross-cutting foundation (Step 2)

| Concern | Location |
|---------|----------|
| Entities + soft delete + audit | `Domain` + EF configurations |
| `Result` / `Error` / paging / specifications | `Application.Common` |
| Exception types | `Domain.Exceptions` |
| External service ports | `Application.Common.Interfaces` (no fake adapters) |
| Domain events | `Domain.Events` + MediatR dispatcher |
| Integration events | `Application.Common.Events` + MediatR publisher |
| Correlation ID + performance logging | `Api.Middleware` |
| Health checks | PostgreSQL, Redis, Hangfire |

## Coding standards

See [14-Coding-Standards.md](14-Coding-Standards.md).
