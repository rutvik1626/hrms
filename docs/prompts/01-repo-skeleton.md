# 01 — Repository & solution skeleton

## Goal
Create the HRMS monorepo skeleton so every later prompt has a consistent place to put code, a working local infrastructure, and CI that builds and tests everything.

## Read first
`CLAUDE.md`; `docs/HRMS-Requirements.md` → Stage 3 "Monorepo layout", Stage 9 "Local development", "Compose files", "Containers", Stage 10 "Test strategy".

## Scope
**In**
- `Hrms.sln` with solution folders; empty projects for BuildingBlocks (`Hrms.Common`, `Hrms.Domain.Abstractions`, `Hrms.Persistence`, `Hrms.Messaging`, `Hrms.Messaging.Contracts`, `Hrms.Security`, `Hrms.Web`, `Hrms.Storage`), the 7 services × 4 layers, `Hrms.Gateway`, and test projects (`tests/Hrms.Testing` shared fixtures, `tests/<Svc>.UnitTests`, `tests/<Svc>.IntegrationTests`, `tests/Architecture.Tests`).
- `Directory.Build.props` (net8.0, nullable, implicit usings, warnings as errors, analyzers), `Directory.Packages.props` (central package management, pinned versions), `.editorconfig`, `global.json`.
- Each service Api: minimal `Program.cs` with `/health/live` only (real wiring comes in 02).
- `deploy/docker-compose.yml` + `docker-compose.override.yml` with **`infra` profile**: Postgres 16 (init script creating the 7 service DBs + `hrms_jobs` and `<svc>_owner` / `<svc>_app` roles), RabbitMQ (management), Redis 7, S3-compatible store (SeaweedFS, see ADR 0002; + bucket init), Seq, Mailpit, pgAdmin. Named volumes, pinned image versions.
- `deploy/.env.example` documenting every variable.
- Angular app scaffold in `src/Web/hrms-web` (latest stable, standalone, zoneless, strict, routing, SCSS, Vitest) — app shell only.
- Flutter app scaffold in `src/Mobile/hrms_mobile` with `dev/staging/prod` flavor entrypoints — default screen only.
- `.github/workflows/ci-backend.yml`, `ci-web.yml`, `ci-mobile.yml` (build + test only, path filters), `.github/dependabot.yml`, PR template.
- `tests/Architecture.Tests` with NetArchTest rules: Domain → no Infrastructure/EF refs; Application → no Api refs; no project in `Services/A` references `Services/B`.
- `README.md`: prerequisites, `docker compose --profile infra up -d`, how to run each part.
- `docs/adr/0001-record-architecture-decisions.md` (ADR template).

**Out** — real business code, auth, Dockerfiles for prod (prompt 19).

## Acceptance criteria
- `dotnet build` and `dotnet test` pass at root; architecture tests pass.
- `docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.override.yml --profile infra up -d` brings all infra up healthy; each service DB and role exists.
- `ng build` and `flutter analyze` pass.
- CI workflows are valid and green on a PR.

## Verify
```bash
dotnet build && dotnet test
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.override.yml --profile infra up -d
docker compose -f deploy/docker-compose.yml ps
cd src/Web/hrms-web && npm ci && npx ng build
cd src/Mobile/hrms_mobile && flutter analyze
```

## Ask me first
- GitHub org/repo name and the product/brand name to use (namespaces stay `Hrms.*`).
- Bundle id / application id for the mobile app (e.g. `com.<company>.hrms`).
