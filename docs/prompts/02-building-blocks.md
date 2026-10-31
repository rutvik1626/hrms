# 02 — Building blocks (shared libraries)

## Goal
Implement the shared libraries every service uses, so tenancy, security, persistence, messaging, errors and observability are solved **once** and services only add business code.

## Read first
`CLAUDE.md`; requirements → Stage 3 "Key patterns", Stage 4 (all), Stage 5 "Conventions", "Error format", "Paging, filtering, sorting", "Concurrency, idempotency, async work", "Files", "Integration event conventions", Stage 6 "Authorization (implements Stage 2)", Stage 9 "Logging & monitoring", Stage 10 "Mandatory cross-cutting test suites".

## Scope — deliverables per library
**Hrms.Common**: `Result`/`Result<T>`, `Error` (code, message, type), `PagedResult<T>`, `CursorPage<T>`, paging/sort request models with whitelist sorting, `IClock`, `IIdGenerator` (UUID v7 via `UUIDNext` or `Medo.Uuid7`), guard helpers.

**Hrms.Domain.Abstractions**: `Entity`, `AggregateRoot` (domain events), `ITenantOwned`, `IAuditable`, `ISoftDeletable`, `IEffectiveDated`, value objects (`Money` INR, `DateRange`, `EmailAddress`, `PhoneNumber` E.164 India default).

**Hrms.Persistence**:
- `HrmsDbContextBase`: snake_case, UUID v7 key generation, `xmin` concurrency, global query filters (tenant + soft delete), automatic tenant stamping (immutable), audit columns.
- `SaveChanges` interceptor writing `audit_log` (JSONB diff; sensitive properties marked `[Sensitive]` logged as `***changed***`).
- `TenantConnectionInterceptor`: `set_config('app.tenant_id', …)` on connection open.
- Migration helpers: `migrationBuilder.EnableRowLevelSecurity("table")` extension generating ENABLE/FORCE RLS + policy SQL; grants for `<svc>_app`.
- `IFieldEncryptor` (AES-256-GCM, key id prefix) + HMAC hash for searchable sensitive fields; EF value converter.
- Idempotency store (table + filter) for `Idempotency-Key`.

**Hrms.Messaging / Contracts**: MassTransit + RabbitMQ setup extension, EF outbox/inbox, envelope header filters (tenantId, correlationId, userId, causationId), consume filter that sets tenant context, retry/redelivery policy per Stage 5. Contracts project with base `IntegrationEvent` record.

**Hrms.Security**: JWT bearer setup (OpenIddict validation), `ICurrentUser` / `ICurrentTenant`, `[HasPermission]` + dynamic policy provider + handler reading permissions from Redis by `sub + perm_ver` (with fallback fetch from identity `/internal`), `IDataScopeResolver` abstraction + scope model, service-to-service client-credentials token handler, impersonation (`act` claim) awareness.

**Hrms.Web**: `AddHrmsService()` / `UseHrmsService()` one-call bootstrap: controllers, API versioning, ProblemDetails mapping from `Result` errors (status per error type, `code`, `traceId`), FluentValidation pipeline behavior, MediatR + logging/validation/transaction behaviors, Swagger (dev/staging), health checks (`/health/live`, `/health/ready` for DB/RabbitMQ/Redis/S3), Serilog (CLEF → Seq, enrichers, PII masking), OpenTelemetry tracing → OTLP, correlation-id middleware, ETag/If-Match helpers, response compression, security headers.

**Hrms.Storage**: `IFileStorage` (S3 API; SeaweedFS locally per ADR 0002) with presigned PUT/GET, file record model (status Pending/Scanning/Available/Rejected), type/size allow-list, ClamAV scan hook (background).

**tests/Hrms.Testing**: Testcontainers fixtures (Postgres with roles + RLS, RabbitMQ, Redis, SeaweedFS S3), `HrmsWebAppFactory<T>`, test auth handler issuing users with given permissions/scope/tenant, reusable suites: **tenant isolation**, **permission matrix** (all endpoints decorated), **idempotency**, **concurrency**, **audit**.

**Sample**: a throwaway `Sample` aggregate in one service (e.g. corehr `Note`) proving the whole pipeline end-to-end, then removed or kept as a test-only fixture.

## Out
Business entities, OpenIddict server (03), gateway (04).

## Acceptance criteria
- Sample endpoint: create/read/update/delete with tenant isolation proven at API **and** DB level (RLS returns 0 rows for other tenant when queried as `<svc>_app`).
- Unauthenticated → 401; missing permission → 403; other tenant's id → 404.
- Replayed idempotency key returns same response; stale `If-Match` → 412.
- Audit row written with masked sensitive field; logs in Seq with tenant/correlation ids and no PII.
- Outbox publishes an event consumed idempotently by a test consumer.
- Library unit tests ≥ 80% coverage.

## Verify
```bash
dotnet test tests/Hrms.Testing tests/Architecture.Tests
dotnet test --filter Category=Integration
```
Open Seq at http://localhost:5341 and find a request by correlation id.

## Ask me first
- Any deviation from the listed packages (MassTransit licensing: confirm the version line we pin is under a licence acceptable to you).
