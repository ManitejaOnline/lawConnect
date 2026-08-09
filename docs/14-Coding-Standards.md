# 14 Coding Standards

These rules apply to the entire LawConnect project.

## Architecture

- Follow Clean Architecture strictly.
- Never violate dependency direction: `Api` → `Application`/`Infrastructure` → `Domain`.
- Domain contains only business rules and entities.
- Business logic belongs in Application (CQRS handlers).
- Controllers must only orchestrate (no business logic).
- Infrastructure depends on abstractions defined in Application.
- Prefer composition over inheritance.

## Code quality

- Never generate duplicate logic. Refactor before adding similar code.
- If a new feature overlaps existing functionality, reuse or refactor instead of copying.
- Use async/await throughout I/O-bound code. Always accept `CancellationToken` on public async APIs.
- No TODOs, placeholders, mock implementations, or commented-out code.
- No magic strings — use constants and options classes.

## Documentation

- Every public method requires XML documentation.
- Every API endpoint requires Swagger documentation.

## Validation and errors

- Every command/query requires FluentValidation.
- Use shared exception types and `Result`/`Error` models consistently.

## Data and tests

- Every database change requires an EF Core migration.
- Every feature must include unit tests (and integration tests where applicable).

## Completion gate

Before completing any step:

1. `dotnet build` succeeds with **zero warnings**.
2. `dotnet test` passes.
3. No compiler warnings remain.
