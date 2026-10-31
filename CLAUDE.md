# CLAUDE.md — HRMS monorepo rules

Multi-tenant SaaS HRMS for Indian SMEs. **Source of truth: `docs/HRMS-Requirements.md`.** Read the sections a task names before writing code. If code and the requirements disagree, stop and ask — do not silently pick one.

## Stack
.NET 8 microservices (Clean Architecture + CQRS) · YARP gateway · PostgreSQL 16 (DB per service, RLS) · RabbitMQ + MassTransit (outbox/inbox) · Redis · Hangfire · OpenIddict · Angular (standalone, signals, zoneless) + PrimeNG + Bootstrap 5 (grid/utilities only) · Flutter (Riverpod, go_router, drift) · Docker Compose · AWS ap-south-1 · GitHub Actions · Serilog → Seq.

## Layout
```
src/BuildingBlocks/Hrms.*          shared libraries — change with care, they affect every service
src/Services/<Svc>/Hrms.<Svc>.{Domain,Application,Infrastructure,Api}
src/Gateway/Hrms.Gateway
src/Web/hrms-web                   Angular
src/Mobile/hrms_mobile             Flutter
tests/                             unit, integration, golden, e2e, load
deploy/                            compose files, nginx, scripts, terraform
docs/                              requirements, ADRs, security, runbooks, perf
```
Services: identity, corehr, time, payroll, workflow, expense, talent. **A service never references another service's projects or database.** Cross-service data comes from integration events (local projections) or `/internal` HTTP calls via typed clients.

## Backend rules (.NET)
- Layers: Domain (entities, value objects, domain events, no infra deps) → Application (MediatR commands/queries, FluentValidation, DTOs, interfaces) → Infrastructure (EF Core, consumers, external clients) → Api (thin controllers).
- Handlers return `Result<T>`; errors carry a stable `code` (e.g. `leave.insufficient_balance`) mapped to RFC 7807 ProblemDetails by `Hrms.Web`.
- Every endpoint has `[HasPermission("<Module>.<Resource>.<Action>")]` or explicit `[AllowAnonymous]`. New permissions are added to the identity seed in the same change.
- Data scope is applied in query handlers via `IDataScopeResolver` — never trust client-supplied filters for authorization.
- Tenant-owned entities implement `ITenantOwned`; never set or filter `tenant_id` by hand; never call `IgnoreQueryFilters()` outside platform code reviewed for it.
- IDs: UUID v7 via `IIdGenerator`. Time: `IClock` (UTC), business dates as `DateOnly`.
- Money: `decimal`, `numeric(18,2)`; never `double`/`float` for money or rates.
- EF: snake_case naming, `xmin` concurrency, migrations per service (`dotnet ef migrations add <Name> -p Infrastructure -s Api`), raw SQL in migrations for RLS policies/grants.
- Messaging: publish integration events only through the outbox; consumers are idempotent; contracts in `Hrms.Messaging.Contracts`, past-tense names, additive changes only.
- POST endpoints that create records or move money accept `Idempotency-Key`; updates require `If-Match`.
- Logging: Serilog structured templates (`{EmployeeId}`), never string interpolation; **never log PII, tokens, salaries, bank/Aadhaar/PAN**.
- Sensitive fields (Aadhaar, PAN, bank account, passport) go through `IFieldEncryptor`.
- Async/await all the way; pass `CancellationToken`; no `.Result`/`.Wait()`.

## Web rules (Angular)
- Standalone components, `OnPush`, signals, `input()`/`output()`, new control flow, `inject()`. No NgModules, no `any`.
- State: `@ngrx/signals` SignalStore per feature in `data-access/`.
- **PrimeNG for every UI component**; Bootstrap classes only for grid/spacing/flex. No Bootstrap JS/components, no hard-coded colours (use theme tokens).
- Backend calls only via the generated client in `src/app/api/` (never edit it; regenerate).
- All user-visible text via Transloco keys (`feature.page.key`); formats via shared `en-IN` pipes (₹ lakh grouping, `dd-MMM-yyyy`).
- Permission checks: `*hasPermission` directive + `canMatch` guards; the server is still the authority.
- Reuse `shared/ui` patterns (list page, detail page, form, wizard, approval timeline, audit history) before creating new ones.

## Mobile rules (Flutter)
- Feature-first: `features/<name>/{data,domain,presentation}`; Riverpod with codegen (`@riverpod`); go_router routes; freezed models.
- Backend calls only via generated `packages/hrms_api`; repositories map DTOs to domain models and return sealed `Result` types.
- Every screen handles loading / empty / error / offline states; strings via ARB; no business logic in widgets.
- Location only when the user punches — never background location. Never store tokens outside `flutter_secure_storage`.

## Definition of done (every task)
1. Code follows the rules above and the requirements sections cited by the task.
2. Tests added: unit for rules; integration for endpoints (including tenant-isolation + permission tests via `tests/Hrms.Testing` fixtures); golden tests for payroll changes.
3. `dotnet build` / `dotnet test`, `npm run lint && npm test`, `flutter analyze && flutter test` pass for the touched parts.
4. Migrations added and applied cleanly on an empty DB; seed data updated (permissions, defaults).
5. OpenAPI updated; generated clients regenerated if the API changed; no unversioned breaking change.
6. i18n keys added for new text; no hard-coded strings.
7. Short summary of what changed, what was not done, and any open question.

## Never
- Commit secrets, `.env`, real employee data, or production connection strings.
- Disable RLS, auth, validation, or tests to make something pass.
- Add a new package/framework without stating why (prefer what the stack already uses).
- Hand-edit generated code (`src/app/api/`, `packages/hrms_api/`, EF migration snapshots).
- Put tenant id, PII, or tokens in URLs or logs.
