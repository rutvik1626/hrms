# HRMS

Multi-tenant HRMS SaaS for Indian SMEs: Angular web, Flutter mobile, .NET 8 microservices, PostgreSQL, Docker.

- **Requirements & decisions:** [docs/HRMS-Requirements.md](docs/HRMS-Requirements.md)
- **Build prompts (run in order):** [docs/prompts/00-HOW-TO-USE.md](docs/prompts/00-HOW-TO-USE.md)
- **Architecture decisions:** [docs/adr/](docs/adr/)
- **Coding rules (for people and Claude Code):** [CLAUDE.md](CLAUDE.md)

## Prerequisites
| Tool | Version |
|---|---|
| .NET SDK | 8.0.1xx (pinned by `global.json`; newer SDKs can be installed alongside) |
| Node.js | 22 LTS (npm 10+) |
| Angular CLI | 22 (`npm i -g @angular/cli@22`, optional — `npx ng` works too) |
| Flutter | stable (3.24+) — then see [src/Mobile/hrms_mobile/README.md](src/Mobile/hrms_mobile/README.md) |
| Docker Desktop | with Compose v2 |
| EF Core tools | `dotnet tool install -g dotnet-ef --version 8.*` |

## Repository layout
```
src/BuildingBlocks/   shared libraries (Hrms.Common, .Persistence, .Messaging, .Security, .Web, .Storage, ...)
src/Services/<Svc>/   Identity, CoreHr, Time, Payroll, Workflow, Expense, Talent
                      each: Hrms.<Svc>.Domain / .Application / .Infrastructure / .Api
src/Gateway/          Hrms.Gateway (YARP)
src/Web/hrms-web/     Angular app
src/Mobile/hrms_mobile/ Flutter app
tests/                unit, integration, architecture tests + shared fixtures (Hrms.Testing)
deploy/               docker compose files, postgres init, local S3 config
docs/                 requirements, prompts, ADRs
```

## Local infrastructure
```bash
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.override.yml --profile infra up -d
```
Defaults work without a `.env`; to override, copy `deploy/.env.example` to `deploy/.env`.

| Service | Address | Credentials (dev only) |
|---|---|---|
| PostgreSQL 16 | `localhost:5432` | superuser `postgres` / `postgres_dev`; per-service `<svc>_owner` / `<svc>_app` / `hrms_dev` |
| RabbitMQ | `localhost:5672`, UI http://localhost:15672 | `hrms` / `hrms_dev`, vhost `hrms` |
| Redis | `localhost:6379` | — |
| S3 (SeaweedFS) | http://localhost:8333, bucket `hrms-files` | `hrms` / `hrms_dev_secret` |
| Seq (logs) | UI http://localhost:5341, ingest `:5342` | no auth locally |
| Mailpit | SMTP `localhost:1025`, UI http://localhost:8025 | — |
| pgAdmin | http://localhost:5050 | `admin@example.com` / `hrms_dev` |

Databases: `hrms_identity`, `hrms_corehr`, `hrms_time`, `hrms_payroll`, `hrms_workflow`, `hrms_expense`, `hrms_talent`, `hrms_jobs`.
Each `<svc>_owner` owns its database (runs migrations); each `<svc>_app` can only read/write data in its own database (no DDL, no RLS bypass).

Reset everything (deletes all local data): `docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.override.yml --profile infra down -v`

## Build & test
```bash
dotnet build Hrms.sln
dotnet test Hrms.sln
```
Run a service (ports: gateway 5000, identity 5101, corehr 5102, time 5103, payroll 5104, workflow 5105, expense 5106, talent 5107):
```bash
dotnet run --project src/Services/CoreHr/Hrms.CoreHr.Api
```

Web:
```bash
cd src/Web/hrms-web
npm ci
npx ng serve        # http://localhost:4200
npx ng test --watch=false
```

Mobile: see [src/Mobile/hrms_mobile/README.md](src/Mobile/hrms_mobile/README.md).

## CI
GitHub Actions (`.github/workflows/`): `ci-backend`, `ci-web`, `ci-mobile`, each triggered only by changes under its own paths. Dependabot updates run weekly.
