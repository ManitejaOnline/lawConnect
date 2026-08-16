# Render deployment (MVP)

Public HTTPS API from GitHub → Render Docker Web Service + PostgreSQL + Key Value (Redis).

Do not commit secrets. Local LAN (`192.168.x.x`) and Cloudflare quick tunnels are development-only.

Push the latest `master` (including `Dockerfile`, `render.yaml`, and Production config) to
`https://github.com/ManitejaOnline/lawConnect.git` before creating the Web Service.

## Apply migrations (no wipe)

Production startup runs `Database.Migrate()` when `Database__ApplyMigrations=true` (default in Production). That applies **pending** EF Core migrations only. It does **not** drop or recreate the database.

If the first deploy already applied them, you do not need to run this again.

From your machine (SDK required), against Render’s **External** Postgres URL:

```bash
dotnet ef database update \
  --project src/LawConnect.Infrastructure \
  --startup-project src/LawConnect.Api \
  --connection "<EXTERNAL_DATABASE_URL>"
```

Never run `EnsureDeleted`, drop-database, or recreate.

## Health

`GET https://<service>.onrender.com/health` → `200` and body `Healthy`.

## Dashboard steps

1. Create PostgreSQL (`lawconnect`, same region as the API).
2. Create Redis / Key Value (private, same region).
3. Create a Web Service.
4. Connect `ManitejaOnline/lawConnect`.
5. Runtime: Docker.
6. Root directory empty; Dockerfile path `Dockerfile`; context `.`.
7. Set environment variables (see `.env.example` Render section). Mark secrets as secret.
8. Health check path: `/health`.
9. Deploy.
10. Confirm migrations via logs (`Applying migration` / no pending) or the `dotnet ef` command above.
11. Open `https://<service>.onrender.com/health`.

Do not set `ASPNETCORE_ENVIRONMENT=Docker` on Render (that enables demo seed and `/api/auth/dev-login`).
