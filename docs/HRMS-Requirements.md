# HRMS — Requirements & Decisions (living document)

> Single source of truth for all build stages. Every Claude build prompt references this file.
> Last updated: 2026-10-31

## Tech Stack
| Layer | Choice |
|---|---|
| Web | Angular (latest LTS), PrimeNG (theme + components), Bootstrap 5 (grid/utilities only) |
| Backend | ASP.NET Core .NET 8 Web API, microservices |
| Database | PostgreSQL |
| Mobile | Flutter (Android + iOS) |
| Containers | Docker, Docker Compose |

---

## Stage 0 — Product Foundation ✅
| Topic | Decision | Implication |
|---|---|---|
| Business model | **Multi-tenant SaaS** | `TenantId` on every tenant-owned row; EF Core global query filters + PostgreSQL RLS; super-admin portal; subscription plans gate modules |
| Target size | **SME, ≤ 1,000 employees/tenant** | Shared Postgres cluster, DB-per-service, no sharding; Hangfire for background jobs |
| Compliance | **India** | PF, ESI, Professional Tax (state-wise), TDS (old/new regime), Form 16, LWF, Gratuity; state-wise holidays & leave rules |
| Hosting | **Cloud VM + Docker Compose** | Gateway + services on 1–2 VMs; stay 12-factor / K8s-ready for later migration |

---

## Stage 1 — Modules & Features ✅

### Release plan
Everything below is in scope for Phase 1, delivered in sub-releases so each one is shippable and sellable:

| Release | Modules | Why this order |
|---|---|---|
| **1A** | Platform (tenancy, auth, RBAC, workflow engine, notifications) + Core HR | Everything depends on it |
| **1B** | Attendance (all capture modes) + Leave | Daily-use features; feeds payroll |
| **1C** | Payroll (India) | Needs attendance + leave data (LOP, OT) |
| **1D** | Expenses + Assets | Expense reimbursements flow into payroll |
| **1E** | Recruitment + Onboarding/Exit | Onboarding creates employees in Core HR; exit triggers F&F settlement in payroll |
| **Phase 2** | Performance (goals/KPIs, appraisal cycles, 360 feedback) | |

### Platform (cross-cutting)
- Tenant sign-up, subscription plan, module enablement, tenant settings (logo, financial year, locale)
- Users, roles, permissions (see Stage 2)
- **Configurable multi-level approval workflow engine** (shared by leave, regularization, expenses, assets, offers)
  - Per-tenant workflow definitions per request type
  - Approver types: reporting manager, Nth-level manager, department head, specific role (HR/Finance), specific user
  - Conditions: amount / days thresholds, department, location, employee grade
  - Actions: approve, reject, send back, delegate; auto-escalation after SLA; approver-on-leave delegation
- Notifications: in-app, email, push (FCM/APNs); templates per tenant
- Audit log of all changes (who, what, when, before/after)
- File storage (documents, receipts, selfies) — S3-compatible (SeaweedFS in dev — see ADR 0002)

### Core HR
- Organization: legal entities, branches/locations (state for compliance), departments, designations, grades, cost centers
- Employee master: personal, contact, job, bank, statutory IDs (PAN, Aadhaar-masked, UAN, ESIC no.), family/nominees, education, experience
- Reporting hierarchy + org chart
- Employee documents with expiry alerts
- Employee lifecycle events: confirmation, transfer, promotion, salary revision, separation
- Employee self-service profile with change-request approval
- Bulk import (Excel) and export

### Attendance
- Shifts, shift rotation/roster, weekly-off patterns, grace periods, half-day rules
- Capture modes (all enabled, configurable per tenant/location/employee):
  - **Mobile GPS + geofence** (multiple geofences per location; mock-location detection)
  - **Mobile selfie** (stored with punch; optional face match against profile photo)
  - **Biometric device sync** (ZKTeco/eSSL via ADMS push or pull agent)
  - **Web check-in** (optional IP whitelist)
- Attendance processing: daily status (Present/Absent/Half-day/WO/Holiday/Leave), late-in/early-out, overtime
- Regularization requests (missed punch) via workflow
- Monthly attendance lock → feeds payroll

### Leave
- Leave types with policies: accrual (monthly/yearly), carry-forward, encashment, lapse, max balance, negative balance, sandwich rule, min/max days, attachment required, applicable gender/grade/location
- State-wise holiday calendars per location; optional holidays
- Apply / cancel / approve via workflow; half-day and hourly leave
- Comp-off earning (from WO/holiday work) and availing
- Leave balance ledger (auditable), year-end processing

### Payroll (India)
- Salary components (earnings/deductions, fixed/variable, taxable flags, formulas), salary structures/templates by grade
- Monthly payroll run: attendance/LOP import → computation → review → lock → publish
- Statutory: PF (with ceilings, VPF), ESI, Professional Tax (state slabs), LWF, TDS (old vs new regime, investment declarations + proof submission), Gratuity
- Arrears, one-time payments/deductions, loans & advances with EMI recovery
- Payslips (PDF), bank transfer file, statutory reports (PF ECR, ESI, PT, Form 16 / 24Q data)
- Full & Final settlement on exit

### Expenses
- Expense categories with limits per grade; mileage
- Claims with receipt upload (mobile camera), multi-currency optional
- Workflow approval → reimbursement via payroll or direct payout
- Advance requests and settlement

### Assets
- Asset catalog and inventory (serial no., purchase info, status)
- Allocation/return to employees, acknowledgement, damage/loss recording
- Asset requests via workflow; asset recovery as an exit-clearance item

### Recruitment & Onboarding / Exit
- Job requisitions (with approval), job openings, careers page
- Candidates, resume upload, pipeline stages (Kanban), interview scheduling + feedback scorecards
- Offer letters (templates, approval, e-acceptance)
- Pre-boarding portal (candidate uploads documents) → convert candidate to employee
- Onboarding checklists (tasks per department: IT, Admin, HR)
- Exit: resignation via workflow, notice period, exit checklist/clearance, exit interview → F&F

### Phase 2 — Performance
- Goals/KPIs/OKRs, appraisal cycles, self + manager + 360 reviews, ratings, normalization, outcomes → salary revision

---

## Stage 2 — Roles & Permissions ✅

### Decisions
| Topic | Decision |
|---|---|
| Role model | Seeded **system roles** (non-deletable) + tenant-defined **custom roles** |
| Authorization | **Permission-based**, not role-based: code checks permissions; roles are just bundles |
| Data scope | Granted **per role assignment** (user + role + scope) |
| Salary data | Guarded by separate `Payroll.*` / `Employee.Salary.View` permissions; hidden (not just disabled) otherwise |
| Multi-role | A user can hold multiple role assignments; effective permissions = union, each with its own scope |
| Impersonation | Platform support may impersonate a tenant user only with tenant-admin consent, time-boxed (e.g. 30 min), read-only by default, fully audited, banner shown in UI |

### Permission format
`<Module>.<Resource>.<Action>` — e.g. `Leave.Request.Approve`, `CoreHr.Employee.Edit`, `Payroll.Run.Lock`, `Employee.Salary.View`.
Actions: `View`, `Create`, `Edit`, `Delete`, `Approve`, `Export`, `Import`, `Configure`, plus module-specific ones (`Run`, `Lock`, `Publish`).

### Data scopes
`Own` · `DirectReports` · `AllReports` (recursive) · `Department` · `Location` · `LegalEntity` · `Tenant` (all)
A role assignment can carry a scope value, e.g. `HR Manager @ Location=Mumbai`.

### System roles
| Role | Level | Typical permissions (default scope) |
|---|---|---|
| Platform Super Admin | Platform | Tenants, plans, subscriptions, platform settings, impersonation (with consent) |
| Platform Support | Platform | Read-only tenant diagnostics, impersonation (with consent) |
| Tenant Admin | Tenant | Everything in tenant incl. settings, roles, workflows, billing (Tenant) |
| HR Admin | Tenant | Core HR, Attendance, Leave, Onboarding/Exit, policies (Tenant or Location) — **no salary** |
| Payroll Admin | Tenant | Salary structures, payroll runs, statutory, tax declarations, F&F (Tenant/LegalEntity) |
| Finance | Tenant | Expense payout, advances, asset finance views, payroll reports read-only |
| Recruiter | Tenant | Requisitions, jobs, candidates, interviews, offers |
| Manager | Tenant | Team view, approvals, team attendance/leave, team reports (AllReports) |
| Employee | Tenant | Self-service: profile, attendance, leave, payslips, tax declarations, expenses, assets (Own) |
| Interviewer | Tenant | Assigned interviews + feedback only |

> "Manager" is also **implicit**: anyone with direct reports automatically gets the Manager permissions scoped to their reports.

### Enforcement points
1. **Gateway / API** — JWT carries user, tenant and role-assignment ids; each service resolves permissions (cached in Redis) and checks via `[HasPermission("Leave.Request.Approve")]` policies.
2. **Data layer** — scope is turned into a query filter (e.g. `EmployeeId IN (reports of X)`); tenant isolation via EF global filter + Postgres RLS.
3. **Field level** — salary/statutory-ID fields are removed by response shaping when the permission is missing.
4. **UI (web + mobile)** — menus, routes, buttons hidden using the permission list from `/me/permissions`; never relied on for security.

## Stage 3 — Microservices Architecture ✅

### Decisions
| Topic | Decision |
|---|---|
| Decomposition | **7 services + gateway**, split by business capability |
| Async messaging | **RabbitMQ + MassTransit**, integration events, **transactional outbox/inbox** (MassTransit EF outbox) |
| Sync calls | Kept to a minimum; HTTP via typed clients + Polly resilience (retry, circuit breaker, timeout) |
| Gateway | **YARP** (.NET 8) |
| Code structure | **Clean Architecture + CQRS** (MediatR, FluentValidation), one monorepo |
| Background jobs | **Hangfire** (Postgres storage) per service that needs it (Time, Payroll, Workflow) |
| Cache | **Redis** (permission cache, lookups, rate-limit counters) |
| File storage | S3-compatible (**SeaweedFS** dev — ADR 0002 / AWS S3 in prod), presigned URLs |

### Services
| Service | Owns | Publishes (examples) | Consumes (examples) |
|---|---|---|---|
| **identity** | Tenants, plans, users, roles, permissions, tokens, impersonation | `TenantCreated`, `UserCreated`, `RoleAssignmentChanged` | `EmployeeCreated` (create login), `EmployeeSeparated` (disable login) |
| **corehr** | Org structure, employees, documents, lifecycle | `EmployeeCreated/Updated`, `EmployeeTransferred`, `ReportingManagerChanged`, `EmployeeSeparated` | `CandidateHired`, `TenantCreated` (seed masters) |
| **time** | Shifts, rosters, punches, biometric sync, geofences, attendance processing, leave, holidays, comp-off | `AttendanceMonthLocked`, `LeaveApproved/Cancelled` | `EmployeeCreated` (local projection), `WorkflowCompleted` |
| **payroll** | Components, structures, payroll runs, statutory, TDS & declarations, loans, F&F, payslips | `PayrollPublished`, `PayslipGenerated` | `AttendanceMonthLocked`, `ExpenseApprovedForPayroll`, `EmployeeSeparated` |
| **workflow** | Workflow definitions, instances, tasks/inbox, escalation, delegation, notifications (email/push/in-app), templates | `WorkflowCompleted` (Approved/Rejected), `NotificationSent` | `WorkflowRequested` from any service, all events that trigger notifications |
| **expense** | Expense categories, claims, advances, assets, allocations | `ExpenseApprovedForPayroll`, `AssetAllocated/Returned` | `WorkflowCompleted`, `EmployeeSeparated` (asset recovery) |
| **talent** | Requisitions, jobs, candidates, interviews, offers, onboarding/exit checklists (+ Performance in Phase 2) | `CandidateHired`, `ExitClearanceCompleted` | `WorkflowCompleted`, `EmployeeSeparated` |
| **gateway** | Routing, JWT validation, rate limiting, CORS, correlation ID | — | — |

### Key patterns
- **Local read models**: services keep a small projection of employee data (id, name, manager, dept, location, grade) fed by `corehr` events — no synchronous calls for every request.
- **Workflow integration**: a service creates a request → publishes `WorkflowRequested` → `workflow` routes approvals → publishes `WorkflowCompleted` → owning service updates status. Approver resolution queries the hierarchy from its own projection.
- **Idempotent consumers** (inbox table) and **correlation ID** carried end-to-end.
- **Tenant context** carried in JWT and in every message header.

### Monorepo layout
```
hrms/
├─ src/
│  ├─ BuildingBlocks/            # shared libs (NuGet-style project refs)
│  │  ├─ Hrms.Common             # Result<T>, errors, paging, guard, clock
│  │  ├─ Hrms.Domain.Abstractions# Entity, AggregateRoot, DomainEvent, ITenantOwned, IAuditable
│  │  ├─ Hrms.Persistence        # EF base DbContext, tenant filter, audit, outbox, RLS interceptor
│  │  ├─ Hrms.Messaging          # MassTransit setup, integration event contracts
│  │  ├─ Hrms.Security           # JWT, permission policies, scope resolution, current user/tenant
│  │  ├─ Hrms.Web                # ProblemDetails, versioning, Swagger, health checks, logging
│  │  └─ Hrms.Storage            # S3 abstraction (AWS S3 / SeaweedFS in dev)
│  ├─ Services/
│  │  └─ <Name>/  (Identity, CoreHr, Time, Payroll, Workflow, Expense, Talent)
│  │     ├─ Hrms.<Name>.Domain
│  │     ├─ Hrms.<Name>.Application   # CQRS commands/queries, validators, DTOs
│  │     ├─ Hrms.<Name>.Infrastructure# EF Core, repos, consumers, external clients
│  │     └─ Hrms.<Name>.Api           # controllers, DI, Program.cs, Dockerfile
│  ├─ Gateway/Hrms.Gateway
│  ├─ Web/hrms-web               # Angular
│  └─ Mobile/hrms_mobile         # Flutter
├─ tests/                        # unit, integration (Testcontainers), contract, e2e
├─ deploy/                       # docker-compose.*.yml, nginx, env templates
└─ docs/                         # this file, ADRs, API specs
```

## Stage 4 — Database Architecture ✅

### Decisions
| Topic | Decision |
|---|---|
| Layout | **One PostgreSQL cluster, one database per service** (`hrms_identity`, `hrms_corehr`, `hrms_time`, `hrms_payroll`, `hrms_workflow`, `hrms_expense`, `hrms_talent`) + `hrms_jobs` (Hangfire) |
| DB users | Per service: `<svc>_owner` (runs migrations, owns tables) and `<svc>_app` (runtime, **no BYPASSRLS**, no DDL) |
| Version | PostgreSQL **16** (same major in dev, CI and prod) |
| Primary keys | **UUID v7**, generated in the app (.NET 8 has no built-in v7 → use `UUIDNext` or `Medo.Uuid7` behind an `IIdGenerator`) |
| Prod hosting | **Managed PostgreSQL** (Azure / AWS RDS / DigitalOcean) with automated backups + PITR; Docker Postgres in dev/CI only |
| Audit | **App-level `audit_log`** table per service, written by an EF Core `SaveChanges` interceptor |
| ORM | EF Core 8 + Npgsql; Dapper allowed for heavy read/report queries |

### Conventions
- **snake_case** tables and columns (`EFCore.NamingConventions`), plural table names (`employees`, `leave_requests`)
- Money: `numeric(18,2)`; rates/percent: `numeric(9,4)`; never `float`
- Time: `timestamptz` stored in **UTC**; business dates (attendance date, payroll month, DOB) as `date`; tenant time zone stored in tenant settings (default `Asia/Kolkata`)
- Enums stored as `text` (readable, safe to extend); lookup tables for tenant-configurable lists
- Flexible / config data (policy rules, workflow definitions, custom fields) in `jsonb` with typed C# models
- Every FK indexed; every tenant-owned table has an index starting with `tenant_id`

### Standard columns (base entity)
| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` | UUID v7 PK |
| `tenant_id` | `uuid` | on every tenant-owned table (all except platform tables in identity) |
| `created_at`, `created_by` | `timestamptz`, `uuid` | set by interceptor |
| `updated_at`, `updated_by` | `timestamptz`, `uuid` | set by interceptor |
| `is_deleted`, `deleted_at`, `deleted_by` | `bool`, `timestamptz`, `uuid` | soft delete (master data only) |
| `xmin` | system | **optimistic concurrency** token (Npgsql `UseXminAsConcurrencyToken`) |

### Multi-tenant isolation (defense in depth)
1. **EF Core global query filter** `tenant_id = currentTenant` on all `ITenantOwned` entities; `tenant_id` auto-set on insert and immutable.
2. **PostgreSQL Row-Level Security** on every tenant-owned table:
   ```sql
   ALTER TABLE employees ENABLE ROW LEVEL SECURITY;
   ALTER TABLE employees FORCE ROW LEVEL SECURITY;
   CREATE POLICY tenant_isolation ON employees
     USING (tenant_id = current_setting('app.tenant_id', true)::uuid)
     WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid);
   ```
   A `DbConnectionInterceptor` runs `SELECT set_config('app.tenant_id', @tenantId, false)` on connection open (Npgsql resets session state when connections return to the pool).
3. **Message consumers / Hangfire jobs** set tenant context from the message header / job argument before touching the DB. Cross-tenant platform jobs use a separate, audited `<svc>_platform` role.
4. **Integration tests** assert that tenant A can never read/write tenant B data (run against real Postgres via Testcontainers).

### Deletion & retention
- **Soft delete**: master data (employees, departments, leave types, salary components, assets…) — filtered out by query filter.
- **Never deleted, only reversed/cancelled**: transactions (punches, leave ledger, payroll results, expense claims). Status changes + reversal entries keep history intact.
- **Hard delete**: drafts, temp uploads, expired tokens, outbox/inbox rows after N days.
- Retention: payroll & statutory records ≥ 8 years (Indian tax/labour record keeping); audit log ≥ 3 years then archived; selfie images 90 days (configurable) then purged.
- Tenant off-boarding: export all data → grace period → purge per DB.

### Sensitive data (PII)
- Encrypted at application level (AES-256-GCM, keys in a secrets store, key id stored with value): **Aadhaar, PAN, bank account no., passport**, salary-revision remarks.
- Aadhaar stored **masked** for display (`XXXX-XXXX-1234`) + encrypted full value only if legally required; searchable fields use a separate HMAC hash column.
- Managed Postgres provides encryption at rest + TLS in transit (`sslmode=require`).

### Audit log (per service)
`audit_log(id, tenant_id, entity_type, entity_id, action [Insert/Update/Delete], changes jsonb {field: {old, new}}, user_id, impersonator_id, correlation_id, ip, occurred_at)`
- Written in the same transaction as the change; sensitive fields logged as `"***changed***"` not values.
- Indexed by `(tenant_id, entity_type, entity_id, occurred_at desc)` → powers the "History" tab in the UI.

### Messaging tables (per service DB)
MassTransit EF **outbox** (`outbox_message`, `outbox_state`) and **inbox** (`inbox_state`) tables for exactly-once-effect processing.

### Key entities per database (initial)
| Database | Main tables |
|---|---|
| `hrms_identity` | tenants, plans, subscriptions, tenant_modules, tenant_settings, users, user_logins, refresh_tokens, roles, permissions, role_permissions, user_role_assignments (with scope), devices (push tokens), impersonation_sessions |
| `hrms_corehr` | legal_entities, locations, departments, designations, grades, cost_centers, employees, employee_addresses, employee_bank_accounts, employee_statutory, employee_family, employee_education, employee_experience, employee_documents, employee_lifecycle_events, profile_change_requests, custom_field_definitions |
| `hrms_time` | employee_projection, shifts, shift_rosters, weekly_off_patterns, geofences, attendance_policies, punches, biometric_devices, attendance_days, regularization_requests, holiday_calendars, holidays, leave_types, leave_policies, leave_balances, leave_ledger, leave_requests, comp_off_credits, attendance_month_locks |
| `hrms_payroll` | employee_projection, salary_components, salary_structures, employee_salaries (effective-dated), pay_groups, payroll_runs, payroll_run_employees, payroll_results (line items), statutory_settings (PF/ESI/PT/LWF slabs by state), tax_regime_choices, investment_declarations, investment_proofs, loans, loan_emis, arrears, one_time_payments, fnf_settlements, payslips |
| `hrms_workflow` | workflow_definitions, workflow_steps, workflow_instances, workflow_tasks, delegations, escalation_rules, notification_templates, notifications, notification_deliveries |
| `hrms_expense` | employee_projection, expense_categories, expense_policies, expense_claims, expense_items, receipts, advances, asset_categories, assets, asset_allocations, asset_requests |
| `hrms_talent` | requisitions, job_openings, candidates, applications, pipeline_stages, interviews, interview_feedback, offers, offer_templates, onboarding_templates, onboarding_tasks, exit_requests, exit_checklist_items, exit_interviews |

> Effective-dating (`effective_from`, `effective_to`) used for salary, designation, department, manager and policy assignments so history and back-dated payroll work correctly.

### Migrations
- **EF Core migrations** per service, committed to source control; reviewed as SQL (`dotnet ef migrations script --idempotent`).
- Applied by a dedicated **migrator container** (one per service, runs as `<svc>_owner`) **before** the API starts — never auto-migrate inside the running API in production.
- RLS policies, roles and grants created in migrations via raw SQL.
- Backward-compatible (expand → migrate → contract) changes so rolling deploys never break.
- Seed data: permissions + system roles (identity), Indian statutory defaults (PT slabs per state, PF/ESI rates), default leave types and workflow templates per new tenant (on `TenantCreated`).

### Performance & scale (SME profile)
- PgBouncer / managed connection pooling; per-service `Max Pool Size` tuned.
- Partition-ready large tables (`punches`, `attendance_days`, `audit_log`, `notifications`) by month — enable partitioning when a table passes ~50M rows.
- Reporting: read-replica (managed) for heavy reports; no cross-database joins — cross-service reports built from events into a reporting store later if needed.

### Backup & recovery
- Managed automated daily backups + **point-in-time restore** (≥ 7–35 days).
- Weekly logical `pg_dump` per database to separate object storage (different region) for long-term retention.
- **Restore drill** every quarter; targets: **RPO ≤ 15 min, RTO ≤ 4 h**.

## Stage 5 — API Design ✅

### Decisions
| Topic | Decision |
|---|---|
| Style | REST over HTTPS, JSON, ASP.NET Core **controllers** (consistent filters, versioning, Swagger) |
| URLs | **Resource-based** at gateway: `/api/v1/<resource>`; gateway maps resource → service |
| Versioning | URL major version (`/api/v1`), `Asp.Versioning.Mvc`; additive changes don't bump version; breaking change → `v2` side by side, v1 deprecated with `Sunset` header |
| Lists | **Offset paging** for grids, **cursor paging** for feeds / mobile infinite scroll |
| Clients | **Generated from OpenAPI** in CI — TypeScript (Angular) + Dart (Flutter) |
| Real-time | **SignalR** hub (workflow service) via gateway + **FCM/APNs** push for mobile background |

### Conventions
- JSON **camelCase**; enums as strings; dates `YYYY-MM-DD`; timestamps ISO 8601 UTC (`2026-10-31T09:15:00Z`); money as number with 2 decimals + currency where relevant (`INR` default)
- Resource names plural, kebab-case: `/leave-requests`, `/salary-structures`
- Sub-resources for ownership: `/employees/{id}/documents`
- Actions that aren't CRUD use a verb sub-path with `POST`: `/leave-requests/{id}/cancel`, `/payroll-runs/{id}/lock`, `/workflow-tasks/{id}/approve`
- **Self-service shortcuts** under `/me`: `/me`, `/me/permissions`, `/me/attendance`, `/me/leave-balances`, `/me/payslips`, `/me/tasks`
- IDs are UUID v7 strings; never expose `tenant_id` in URLs (comes from token)
- Status codes: `200` OK, `201` Created (+ `Location`), `202` Accepted (async), `204` No Content, `400` validation, `401`, `403`, `404` (also for other tenant's data — never `403`, don't leak existence), `409` conflict/business rule, `412` precondition failed (stale ETag), `422` domain rule violation, `429` rate limited

### Error format — RFC 7807 ProblemDetails
```json
{
  "type": "https://hrms.example.com/errors/leave.insufficient-balance",
  "title": "Insufficient leave balance",
  "status": 422,
  "code": "leave.insufficient_balance",
  "detail": "Casual Leave balance is 1.5 days; 3 days requested.",
  "errors": { "toDate": ["Must be on or after fromDate."] },
  "traceId": "00-4bf92f...-01"
}
```
- `code` is stable and machine-readable → clients map it to localized messages.
- Validation errors from FluentValidation fill `errors` (field → messages).
- No stack traces / SQL in production responses.

### Paging, filtering, sorting
**Offset (web grids):**
`GET /api/v1/employees?page=1&pageSize=25&sort=-joiningDate,lastName&search=rao&departmentId=...&status=Active`
```json
{ "items": [ ... ], "page": 1, "pageSize": 25, "totalCount": 412 }
```
**Cursor (feeds / mobile):**
`GET /api/v1/me/notifications?limit=20&cursor=eyJpZCI6...`
```json
{ "items": [ ... ], "nextCursor": "eyJpZCI6...", "hasMore": true }
```
- `pageSize`/`limit` max 100 (default 25); `sort` comma-separated, `-` prefix = desc, only whitelisted fields.
- Filters are explicit, typed query params per endpoint (no generic expression language); date ranges as `fromDate`/`toDate`.
- Data scope (Stage 2) is always applied server-side on top of filters.

### Concurrency, idempotency, async work
- **Optimistic concurrency**: `GET` returns `ETag` (from `xmin`); `PUT/PATCH/DELETE` require `If-Match` → `412` if stale.
- Updates: `PUT` for full forms; `PATCH` (JSON Merge Patch) only for small partial updates (e.g. settings toggles).
- **Idempotency-Key** header (client-generated UUID) **required** on create/financial POSTs (punch, leave apply, expense claim, payroll actions); server stores key + response for 24 h and replays it on retry. Mobile sends client-generated UUID v7 ids for offline-created records.
- **Long-running operations** (payroll run, bulk import, Form 16 generation, large exports): `202 Accepted` + `Location: /api/v1/jobs/{jobId}`; job status `Queued/Running/Succeeded/Failed` with progress %, result link; progress also pushed over SignalR.

### Files
- Upload: `POST /api/v1/files/upload-url` → presigned PUT URL (S3) → client uploads directly → client sends returned `fileId` with the business request. Server validates type (allow-list), size (e.g. 10 MB docs, 2 MB selfies), and virus-scans (ClamAV) before marking file `Available`.
- Download: API returns short-lived presigned GET URL after permission check — files are never public.
- Generated documents (payslips, letters, Form 16): PDF via QuestPDF, stored and served the same way.

### Bulk import / export
- Import: upload Excel → `POST /api/v1/imports { type: "Employees", fileId }` → async job → validation report (row-level errors downloadable) → `POST /imports/{id}/commit`.
- Export: `GET ...?format=xlsx|csv` for small sets; async job for large ones. Export permission (`*.Export`) checked separately.

### Mobile-specific endpoints
- `POST /api/v1/attendance/punches/batch` — offline punches synced in bulk (each with client id, device time, GPS, accuracy, mock-location flag, selfie fileId); server-side validation of geofence + time drift.
- `GET /api/v1/me/bootstrap` — one call on app start: profile, permissions, enabled modules, tenant settings, geofences, today's shift, pending task count (reduces round trips on slow networks).
- `POST /api/v1/me/devices` — register FCM/APNs token; `DELETE` on logout.
- Response compression (Brotli/gzip) enabled at gateway.

### Gateway route map (YARP)
| Path prefix(es) | Service |
|---|---|
| `/api/v1/auth`, `/tenants`, `/users`, `/roles`, `/permissions`, `/me/permissions`, `/me/devices` | identity |
| `/api/v1/employees`, `/org`, `/locations`, `/departments`, `/designations`, `/grades`, `/profile-change-requests`, `/me` (profile) | corehr |
| `/api/v1/attendance`, `/shifts`, `/rosters`, `/geofences`, `/leave-*`, `/holidays`, `/comp-offs`, `/me/attendance`, `/me/leave-balances` | time |
| `/api/v1/payroll-*`, `/salary-*`, `/tax-declarations`, `/loans`, `/fnf`, `/me/payslips` | payroll |
| `/api/v1/workflow-*`, `/notifications`, `/me/tasks`, `/me/notifications`, `/hubs/notifications` | workflow |
| `/api/v1/expense-*`, `/advances`, `/assets`, `/asset-*` | expense |
| `/api/v1/requisitions`, `/jobs`, `/candidates`, `/interviews`, `/offers`, `/onboarding*`, `/exits` | talent |
| `/api/v1/files`, `/imports`, `/jobs/{id}` | owning service per type (files: shared storage lib in each service; job status routed by job-id prefix) |
| `/api/v1/me/bootstrap` | gateway aggregation (fan-out to identity + corehr + time + workflow) |

- Gateway: JWT validation, CORS (web origin only), rate limiting (per user + per tenant + stricter on `/auth`), request size limits, `X-Correlation-Id`, security headers, strips any client-sent internal headers.
- **Internal endpoints** (`/internal/...`) for service-to-service calls are **not routed** by the gateway; secured with service tokens (client-credentials).

### Real-time (SignalR)
- Hub: `/hubs/notifications` hosted in workflow service, proxied via gateway (WebSockets enabled); auth via JWT (`access_token` query string for WebSocket handshake only).
- Groups: `user:{userId}`, `tenant:{tenantId}`; Redis backplane when scaled to > 1 instance.
- Server → client events: `notificationReceived`, `taskCountChanged`, `jobProgress`, `approvalStatusChanged`.
- Mobile: SignalR (`signalr_netcore`) while app is in foreground; **FCM** (Android + iOS via APNs) when background/killed — notification taps deep-link to the screen (`hrms://leave-requests/{id}`).

### OpenAPI & client generation
- Each service: Swashbuckle → `/swagger/v1/swagger.json` (dev/staging only; disabled in prod), XML comments, examples, `ProblemDetails` documented for each error code.
- CI merges specs into one gateway-shaped spec (path rewrite) and generates:
  - **Angular**: `ng-openapi-gen` → `src/app/api/` (services + models, standalone-friendly)
  - **Flutter**: `openapi-generator` `dart-dio` → `packages/hrms_api/`
- CI fails if a spec change is **breaking** (`oasdiff breaking`) without a version bump.

### Integration event conventions (RabbitMQ / MassTransit)
- Contracts in `Hrms.Messaging.Contracts`, records, **past-tense names** (`LeaveApproved`), **additive changes only**; breaking change → new `LeaveApprovedV2`.
- Envelope headers: `tenantId`, `correlationId`, `causationId`, `userId`, `occurredAt`, `eventId` (UUID v7).
- Events carry ids + the minimal data consumers need (no full aggregates, no PII beyond what's required).
- Retry policy: immediate ×3 → delayed redelivery (1 m, 5 m, 30 m) → `_error` queue with alert.

### Health & ops endpoints
`/health/live`, `/health/ready` (DB, RabbitMQ, Redis checks) per service — not routed publicly; `/metrics` (OpenTelemetry/Prometheus) internal only.

## Stage 6 — Authentication & Authorization ✅

### Decisions
| Topic | Decision |
|---|---|
| Token server | **OpenIddict** (OAuth 2.1 / OIDC) inside the identity service + ASP.NET Core Identity for users/passwords |
| Login methods | **Email/username/employee code + password**, **mobile number + SMS OTP**, **Google / Microsoft SSO** (per tenant) |
| MFA | **Required** for Tenant Admin, HR Admin, Payroll Admin, Finance and platform roles; tenant can extend to all users |
| Mobile session | **Long-lived session + biometric/PIN app lock**; 30-day inactivity expiry; remote logout |
| Later (higher plan) | SAML / generic OIDC enterprise SSO |

### Tenant resolution at login
- **Web**: tenant subdomain — `acme.hrms.app` → tenant `acme` (custom domains later). Super-admin portal at `admin.hrms.app`.
- **Mobile**: first screen asks for **company code** (or detects tenant from email domain / phone number lookup). If a phone number exists in several tenants, user picks the company. Choice remembered on device.
- Users are unique **per tenant** (`tenant_id + email`, `tenant_id + phone`, `tenant_id + employee_code`).

### Login flow (one flow for all methods)
- **Authorization Code + PKCE** for both apps. Login, OTP, MFA, SSO, forgot-password and first-time password pages are **hosted by the identity service** (Razor Pages, tenant-branded with logo/colours), so every method, MFA and lockout rule lives in one place.
  - Angular: `angular-auth-oidc-client`
  - Flutter: `flutter_appauth` (opens ASWebAuthenticationSession / Chrome Custom Tab — required for Google/Microsoft SSO anyway)
- Password grant / ROPC is **not** used (deprecated in OAuth 2.1).
- **Account creation**: no self sign-up for employees. Creating an employee (`EmployeeCreated`) creates a user and sends an **invite link** (email or SMS) to set a password / verify phone. Tenant sign-up (company admin) is the only public registration.

### Tokens
| Token | Lifetime | Storage |
|---|---|---|
| Access token (JWT, signed ES256/RS256, keys rotated) | **15 min** | Web: memory only · Mobile: memory |
| Refresh token (opaque, **rotated on every use**, reuse ⇒ whole session revoked) | Web: 12 h absolute, 30 min idle (tenant-configurable) · Mobile: 30 days sliding, 90 days absolute | Web: handled by OIDC lib (in-memory + silent renew) · Mobile: **Keychain / Android Keystore** via `flutter_secure_storage` |
| ID token | 15 min | not sent to APIs |

**Access token claims** (kept small): `sub` (user id), `tid` (tenant id), `sid` (session id), `eid` (employee id), `amr` (pwd/otp/mfa/sso), `perm_ver` (permission version), `act` (impersonator, if any), `aud` = `hrms-api`, `scope`.
Permissions are **not** in the token — services fetch them by `sub + perm_ver` from Redis (populated by identity), so role changes take effect within seconds, not at token expiry.

### Password & OTP rules
- Password: min 10 chars, no composition rules, blocked against common/breached list; hashed by ASP.NET Core Identity (PBKDF2-SHA512, 100k+ iterations). Tenant can tighten (expiry, history).
- Lockout: 5 failed attempts → 15 min lockout (progressive); CAPTCHA after 3 failures on web.
- **SMS OTP**: 6 digits, 5 min validity, max 3 verify attempts, resend after 30 s, max 5 sends/hour per phone + per-IP limits; SMS provider **MSG91** (India, DLT-registered templates per TRAI rules) behind an `ISmsSender` abstraction; email OTP fallback.
- Forgot password: email or SMS link/OTP; all sessions revoked after reset.

### SSO (Google / Microsoft)
- Configured per tenant (Tenant Admin enters allowed domain; domain verified by DNS TXT or admin email confirmation).
- SSO login **only links to an existing user** with matching verified email — no auto-provisioning of employees.
- Tenant can make SSO mandatory (disables password login except for a break-glass admin).
- MFA handled by Google/Microsoft for SSO users (`amr` from IdP honoured).

### MFA
- Methods: **TOTP authenticator app** (preferred), **email OTP**; 10 single-use **recovery codes**.
- Required for privileged roles at next login after the role is assigned; enrolment forced.
- "Trust this device" for 30 days (web); mobile app lock (below) counts as device possession factor for employees.
- **Step-up MFA** (re-verify within last 10 min) for sensitive actions: payroll lock/publish, bank account change, role/permission changes, impersonation approval, data export.

### Mobile session & app lock
- After first login, user sets **biometric** (`local_auth`: Face ID / fingerprint) or a **4–6 digit PIN** (PIN never leaves device; stored as salted hash in secure storage).
- App lock on cold start and after 5 min in background; 5 wrong PINs → wipe tokens, full login required.
- Device registered on login (`devices` table: model, OS, app version, push token, last seen). Users can see their devices; tenant admin can **revoke a device** → its refresh tokens are revoked immediately.
- Root/jailbreak + emulator detection: warn by default, tenant can block; mock-location detection feeds attendance validation.
- Session ends: 30 days inactivity, remote logout, password reset, employee separation, or device revocation.
- Minimum app version enforced via `/me/bootstrap` (force-update screen).

### Session management
- `/me/sessions`: list active sessions/devices, sign out one or all.
- Admin: force logout user, disable user, reset MFA.
- On `EmployeeSeparated` (effective date): user disabled, all sessions revoked, push tokens removed; ex-employee can optionally keep a limited "alumni" login for payslips/Form 16 (tenant setting).

### Service-to-service auth
- **Client credentials** flow (OpenIddict) with per-service client and scope (`corehr.internal`, `time.internal` …); tokens cached until near expiry.
- **Biometric sync agent / devices**: separate client per tenant agent with rotating secret; only scope `time.punches.write`.
- RabbitMQ: per-service credentials + vhost; messages trusted only from the broker, tenant taken from envelope header.

### Authorization (implements Stage 2)
- `Hrms.Security` package: `[HasPermission("Leave.Request.Approve")]` attribute → dynamic policy provider → `PermissionAuthorizationHandler` (reads cached permission set + scopes for `sub`).
- `IDataScopeResolver` turns the caller's scope into EF filters (`EmployeeId IN (...)`, `LocationId = ...`); applied in query handlers, not controllers.
- **Resource-based checks** in handlers: e.g. approver must be the current workflow task assignee; employee can act only on own requests.
- **Segregation of duties**: nobody approves their own request; payroll **maker–checker** (person who runs payroll ≠ person who locks/publishes, when tenant enables it); role admins cannot grant permissions they don't hold.
- Cache invalidation: identity publishes `RoleAssignmentChanged` / bumps `perm_ver` → services drop cached entry.
- UI uses `/me/permissions` only to show/hide — every API still checks.

### Impersonation (support)
- Tenant Admin approves a support request in-app (time window, reason) → platform support uses **token exchange** to get a token with `sub` = target user, `act` = support user, read-only scope by default, max 30 min.
- Banner on every screen while impersonating; all actions audited with `impersonator_id`; step-up MFA for the support user.

### Security audit events
Login success/failure, lockout, MFA enrol/reset, password change/reset, OTP send/verify, SSO link, session revoke, device revoke, role/permission change, impersonation start/end — stored in identity `audit_log` and visible to Tenant Admin (Security log page).

## Stage 7 — Web UI/UX ✅

### Decisions
| Topic | Decision |
|---|---|
| Layout | **Collapsible left sidebar + top bar** |
| Theme | **PrimeNG Aura preset**, per-tenant **primary colour + logo**, user **light/dark** toggle |
| Languages | **English at launch, i18n-ready** (Transloco, runtime switching; no hard-coded strings) |
| State | **Angular signals + `@ngrx/signals` SignalStore** per feature |

### Frontend tech baseline
- Angular **latest stable** at project start (pin exact version), **standalone components only**, **zoneless** change detection, signals, new control flow (`@if`, `@for`), `inject()`, lazy-loaded feature routes.
- PrimeNG (styled mode, Aura preset via `providePrimeNG` + `definePreset`), PrimeIcons.
- Bootstrap 5 — **CSS only, grid + spacing/flex utilities** (`bootstrap-grid` + `bootstrap-utilities`); **no Bootstrap JS, no Bootstrap components** (buttons, modals, forms, tables all come from PrimeNG) to avoid style clashes. Bootstrap reboot excluded; PrimeNG CSS layer order set so PrimeNG wins.
- Auth: `angular-auth-oidc-client` (code + PKCE, silent renew), HTTP interceptors for token, `Idempotency-Key`, `If-Match`, correlation id, ProblemDetails → toast/field errors.
- API: generated client (`ng-openapi-gen`) — never hand-written HTTP calls to the backend.
- Real-time: `@microsoft/signalr`.
- Charts: PrimeNG Chart (Chart.js). Rich text: PrimeNG Editor (offer letter templates). PDF preview: browser native / `ngx-extended-pdf-viewer`.
- Testing: unit tests with the Angular CLI default runner (Vitest), component tests with Testing Library, **Playwright** e2e.
- Lint/format: ESLint (angular-eslint) + Prettier; strict TypeScript.

### Theming
- Design tokens only — no hard-coded colours. Tenant `primaryColor` (from `/me/bootstrap`) → generate 50–950 palette at runtime → `updatePrimaryPalette()`.
- Dark mode: PrimeNG `darkModeSelector: '.app-dark'`; Bootstrap utilities follow `data-bs-theme`; preference stored per user (server) + system default.
- Tenant logo (light + dark variants), favicon, login page branding.
- Density: comfortable default, "compact" option for data-heavy users (HR/Payroll).

### Shell layout
```
┌──────────────────────────────────────────────────────────────────────┐
│ ☰  [Tenant logo]   🔍 Search employees / pages (Ctrl+K)   ✓ Inbox(3) 🔔(5)  🌙  👤 │
├──────────┬───────────────────────────────────────────────────────────┤
│ Home     │ Breadcrumb › Page title                    [Primary action] │
│ My Space │ ───────────────────────────────────────────────────────── │
│ My Team  │                                                           │
│ Core HR  │                    Page content                           │
│ Time     │                                                           │
│ Payroll  │                                                           │
│ Expenses │                                                           │
│ Assets   │                                                           │
│ Talent   │                                                           │
│ Reports  │                                                           │
│ Settings │                                                           │
└──────────┴───────────────────────────────────────────────────────────┘
```
- Menu built from a **route config with required permission + module flag**; items hidden when the user lacks the permission or the tenant's plan lacks the module.
- **Global search (Ctrl+K)**: employees (within scope) + pages/actions.
- **Inbox**: pending approval tasks (count live via SignalR). **Bell**: notifications.
- Impersonation banner and "trial / plan expiring" banner slot above content.
- Responsive: sidebar becomes overlay drawer < 992 px; all ESS screens usable on tablet/phone browsers.

### Navigation by persona
| Menu group | Employee | Manager | HR Admin | Payroll Admin | Finance | Recruiter | Tenant Admin |
|---|---|---|---|---|---|---|---|
| Home (role dashboard) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| My Space (profile, attendance, leave, payslips, tax, expenses, assets, documents) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| My Team (team calendar, attendance, approvals, team reports) | | ✓ | | | | | |
| Core HR / Time / Onboarding & Exit | | | ✓ | | | | ✓ |
| Payroll | | | | ✓ | view reports | | ✓ |
| Expenses & Assets (admin) | | | ✓ assets | | ✓ | | ✓ |
| Talent (recruitment) | | (hiring manager view) | ✓ | | | ✓ | ✓ |
| Reports | own | team | ✓ | ✓ | ✓ | ✓ | ✓ |
| Settings (company, policies, workflows, roles, users, integrations, billing) | | | partial | payroll settings | | | ✓ |

### Dashboards
- **Employee**: check-in/out widget with today's shift, leave balances, upcoming holidays, pending requests status, latest payslip, announcements, birthdays/anniversaries.
- **Manager**: pending approvals, who's in/out/on leave today, team attendance exceptions (late, missed punch), upcoming team leave calendar.
- **HR Admin**: headcount & joiners/leavers trend, attendance today (present/absent/leave %), pending HR tasks (profile changes, onboarding, exits), document expiries, probation confirmations due.
- **Payroll Admin**: current payroll cycle status stepper, attendance lock status, pending declarations/proofs, statutory due dates (PF 15th, ESI 15th, TDS 7th, PT per state).
- **Tenant Admin**: subscription/usage, users & seats, setup checklist (onboarding wizard progress), security log highlights.

### Standard page patterns (build once, reuse everywhere)
| Pattern | Use | Components |
|---|---|---|
| **List page** | employees, requests, assets… | `p-table` lazy (server paging/sort/filter), toolbar (search, filters drawer, saved filters, column chooser, export), row actions menu, bulk select, empty state |
| **Detail page** | employee, payroll run, candidate | header card (avatar, key facts, status tag, actions) + `p-tabs` (Overview, Job, Documents, … **History**) |
| **Form page / dialog** | create/edit | Reactive **typed forms**, `p-floatlabel` fields, inline validation, server errors mapped to fields, dirty-check guard on leave, sticky save bar |
| **Wizard** | tenant setup, payroll run, onboarding, import | `p-stepper`, resumable (state saved server-side) |
| **Request + approval** | leave, regularization, expense, asset | form → submitted → **approval timeline** (`p-timeline`: who, when, comments) → actions (approve/reject/send back with comment) |
| **Approvals inbox** | managers/HR/finance | grouped by type, bulk approve, quick view side panel |
| **Calendar** | attendance, team leave, holidays | month grid with status colours + legend; FullCalendar Angular for team view |
| **Settings page** | policies | left sub-nav + form sections, "effective from" date for policy changes |
| **Import** | bulk upload | template download → upload → validation report → commit |

- Shared UI library `src/app/shared/ui`: page header, status tag, employee picker (scoped autocomplete), date-range picker, amount input (₹), file uploader (presigned), approval timeline, audit history, empty/error/skeleton states, confirm dialog.

### Screen inventory (Phase 1)
- **Platform**: login (hosted), tenant sign-up, setup wizard (company → locations → departments → policies → import employees → invite), company settings, roles & permissions matrix editor, user management, workflow designer (step list + conditions, not drag-drop canvas in v1), notification templates, security log, subscription & billing, super-admin portal (tenants, plans, impersonation requests).
- **Core HR**: employee list, employee 360° profile (tabs: personal, job, compensation*, bank*, statutory*, family, education, experience, documents, assets, history), add employee wizard, org chart, org masters (entities, locations, departments, designations, grades, cost centers), lifecycle actions (confirm, transfer, promote, separate), profile change requests, document expiry tracker, bulk import. (*permission-gated)
- **Time**: my attendance calendar + punch log (map + selfie), team attendance, attendance register (HR), regularization, shifts & roster planner, geofences (map editor), biometric devices, holiday calendars, leave types & policies, apply leave, leave balances, team leave calendar, comp-off, month-end attendance lock.
- **Payroll**: salary components, structures, employee salary assignment/revision, payroll run wizard (inputs → compute → variance review → lock → publish), payslip viewer, statutory settings (PF/ESI/PT/LWF), tax regime + investment declaration (employee), proof verification (payroll), loans & advances, arrears, F&F settlement, bank file, statutory reports.
- **Expenses & Assets**: my claims, new claim (multi-item + receipts), claim approval, finance payout queue, advances, expense policy settings; asset catalog, asset detail, allocate/return, my assets, asset requests.
- **Talent**: requisitions, job openings, careers page (public, tenant-branded), candidate pipeline (Kanban), candidate profile, interview scheduler + feedback form, offer builder + approval, onboarding checklist board, exit requests, exit clearance board, exit interview.
- **Reports**: catalogue of standard reports per module with filters, preview, export (xlsx/pdf).

### Formatting & localisation
- Transloco with scoped translation files per feature (`assets/i18n/<feature>/en.json`); keys like `leave.apply.title`.
- Locale `en-IN`: currency `₹1,25,000.00` (lakh grouping), dates `31-Oct-2026` (tenant-configurable), 24 h or 12 h time per tenant, week starts Monday.
- Server error `code` → translated message; fallback to `title`.

### Accessibility & UX quality
- **WCAG 2.1 AA**: keyboard navigable, visible focus, labels on all inputs, colour not the only status signal (icon + text), contrast checked in both themes.
- Skeleton loaders for lists, optimistic UI only for safe actions, toast for success, inline message for errors.
- Unsaved-changes guard, confirmation for destructive actions, undo where cheap (e.g. withdraw request).
- Every list remembers filters/sort per user (local storage).

### Angular project structure
```
src/app/
├─ core/            # auth, interceptors, error handler, layout shell, menu, permission service,
│                   # tenant/bootstrap store, signalr service, i18n setup, guards
├─ shared/ui/       # reusable presentational components (see above)
├─ shared/util/     # pipes (inrCurrency, dateFmt), validators, helpers
├─ api/             # GENERATED OpenAPI client — do not edit
└─ features/
   ├─ dashboard/
   ├─ me/           # self-service
   ├─ team/
   ├─ core-hr/      # employees/, org/, lifecycle/ …
   ├─ time/         # attendance/, leave/, shifts/, holidays/ …
   ├─ payroll/
   ├─ expense/
   ├─ asset/
   ├─ talent/
   ├─ reports/
   ├─ settings/
   └─ platform-admin/   # super-admin, lazy + separate permission
```
- Each feature: `*.routes.ts` (lazy), `data-access/` (SignalStore + API calls), `pages/` (routed smart components), `components/` (dumb, `input()`/`output()`), `models/` (view models mapped from API DTOs).
- Rules: `OnPush` everywhere, no `any`, no logic in templates, permission checks via `*hasPermission` structural directive + route `canMatch` guard, no direct `localStorage` for tokens.
- Performance budgets: initial bundle < 500 KB gz, lazy chunks per feature, `@defer` for heavy widgets (charts, map, calendar).

## Stage 8 — Flutter Mobile App ✅

### Decisions
| Topic | Decision |
|---|---|
| Scope | **Employee self-service + Manager + HR-lite**; configuration, payroll processing and setup stay web-only |
| Offline | **Offline punch queue + cached read-only data**; all other actions need network |
| Field force | **Not in scope** (no visit logging, no background location tracking) |
| Architecture | **Riverpod (codegen) + go_router + feature-first clean layout** |
| Distribution | **One app for all tenants** ("HRMS" brand) on Play Store + App Store; tenant chosen by company code. White-label builds = later option |

### Tech stack
| Concern | Package |
|---|---|
| State / DI | `flutter_riverpod`, `riverpod_annotation` + `riverpod_generator` |
| Routing / deep links | `go_router` (guards by auth + permission) |
| Models | `freezed`, `json_serializable` |
| API | generated `dart-dio` client (`packages/hrms_api`) + `dio` interceptors (auth refresh, idempotency key, correlation id, ProblemDetails mapping) |
| Local DB (offline queue + cache) | `drift` (SQLite), encrypted with SQLCipher (`sqlcipher_flutter_libs`) |
| Secure storage | `flutter_secure_storage` (tokens, PIN hash, DB key) |
| Auth | `flutter_appauth` (code + PKCE), `local_auth` (biometric) |
| Location | `geolocator` (foreground only, high accuracy) |
| Camera / selfie | `camera` (front camera, in-app capture only), `google_mlkit_face_detection` (on-device face + blink liveness) |
| Maps | `flutter_map` + OpenStreetMap tiles (no per-call cost) |
| Push | `firebase_messaging` (FCM → APNs for iOS) + `flutter_local_notifications` |
| Real-time | `signalr_netcore` (foreground only) |
| Background sync | `workmanager` (Android) / BGTaskScheduler via `workmanager` (iOS) |
| Connectivity | `connectivity_plus` + real reachability check against `/health` |
| Files | `file_picker`, `image_picker` (receipts only), `flutter_image_compress`, `open_filex`, `pdfx` (payslip viewer) |
| i18n | `flutter_localizations` + ARB files (`intl`), same keys as web where possible |
| Crash / monitoring | Firebase Crashlytics (or Sentry) + basic analytics (screen views, no PII) |
| Device integrity | `safe_device` / `flutter_jailbreak_detection`, mock-location flag from `geolocator` |

Minimum OS: **Android 8.0 (API 26)**, **iOS 15**. Target low/mid-range Android (2–3 GB RAM): cold start < 2.5 s, APK/AAB download < 30 MB.

### Feature matrix
| Feature | Employee | Manager | HR (lite) |
|---|---|---|---|
| Check-in / check-out (GPS + geofence + selfie) | ✓ | ✓ | ✓ |
| My attendance calendar, punch history, regularization request | ✓ | ✓ | ✓ |
| Leave: balances, apply, cancel, history, holiday list, comp-off request | ✓ | ✓ | ✓ |
| Payslips (view/download PDF), YTD summary, Form 16 download | ✓ | ✓ | ✓ |
| Tax: regime choice, investment declaration, **proof upload via camera** | ✓ | ✓ | ✓ |
| Expenses: new claim (camera receipt), drafts, status, advances | ✓ | ✓ | ✓ |
| My assets, acknowledge allocation, raise asset request | ✓ | ✓ | ✓ |
| Profile view, change request (address, phone, bank*), documents (view/upload) | ✓ | ✓ | ✓ |
| Notifications, announcements, birthdays/anniversaries, company directory | ✓ | ✓ | ✓ |
| Resignation submission + exit checklist status | ✓ | ✓ | ✓ |
| **Approvals inbox** (leave, regularization, comp-off, expense, asset, profile change) incl. bulk approve | | ✓ | ✓ |
| Team: who's in/out/on leave today, team attendance, team leave calendar, team member profile (scoped) | | ✓ | ✓ |
| Interview feedback (assigned interviews) | Interviewer | ✓ | ✓ |
| HR quick view: employee search (scoped), today's attendance summary by location, flagged-punch review, approve/reject | | | ✓ |
| Dashboard KPIs (headcount, present %, pending tasks) | | | ✓ |

*Bank change always requires approval + step-up MFA.
**Web-only**: org setup, policies, shifts/rosters, payroll run, salary structures, statutory settings, roles, workflows, recruitment pipeline management, reports export, imports.

Menus/tiles are driven by `/me/bootstrap` (permissions + enabled modules), so the same build serves all roles and plans.

### Navigation
Bottom navigation (adapts to role):
| Tab | Employee | Manager / HR |
|---|---|---|
| 1 | **Home** | **Home** |
| 2 | **Attendance** | **Attendance** |
| 3 | **Requests** (leave, regularization, expenses, assets) | **Inbox** (approvals, badge count) |
| 4 | **Pay** (payslips, tax) | **Team** |
| 5 | **More** (profile, documents, assets, directory, settings) | **More** (incl. Requests, Pay) |

Home = big **Check-in / Check-out button** with live shift timer, today's shift, quick actions (Apply leave, New expense, Regularize), leave balance chips, pending items, announcements.

### Screen inventory
1. **Onboarding & auth**: splash (version check) → company code → hosted login (browser tab) → set biometric/PIN → permissions primer (location, camera, notifications) → home. App lock screen. Force-update screen. Maintenance screen.
2. **Home** (role-aware).
3. **Attendance**: punch screen (map with geofence circle + your position + accuracy, selfie camera, confirm), punch result, attendance calendar (colour-coded month), day detail (punches with map/selfie thumbnails, status, late/early), regularization form, pending sync list.
4. **Leave**: balances, apply leave (type, dates, half-day, reason, attachment, live balance + working-days preview), my leaves list, leave detail with approval timeline, cancel, holiday list (with optional-holiday selection), comp-off request.
5. **Pay**: payslip list by month, payslip detail (earnings/deductions breakdown) + PDF download/share, YTD, Form 16, tax regime selection, declarations, proof upload.
6. **Expenses**: claim list, new claim (multiple items; each: category, date, amount, receipt photo, notes; policy-limit warnings), claim detail + timeline, advances.
7. **Assets**: my assets, asset detail + acknowledge, request asset.
8. **Profile**: personal, job, contact, bank (masked), family, documents; edit → change request; my devices/sessions; security (change PIN/biometric, sign out everywhere).
9. **Inbox (Manager/HR)**: grouped list, request detail (with employee context: balance, team calendar conflicts, policy check results), approve/reject/send back with comment, bulk approve.
10. **Team**: today board (in/out/leave/absent), member list → member profile (scoped) → attendance & leave.
11. **HR-lite**: employee search, location attendance summary, flagged punches review (map + selfie + reason), HR dashboard.
12. **Common**: notifications centre, announcements, directory (name, designation, dept, phone/email tap-to-call), resignation, settings (language, theme light/dark/system, notification preferences), help/about.

### Core workflows & business logic

#### A. Check-in / check-out
```
Tap Check-in
 → app ensures location + camera permission (explain why if denied)
 → get GPS fix (high accuracy, timeout 15 s, keep best fix ≤ 50 m accuracy)
 → client checks:   mock location? → block with message
                    inside any assigned geofence? (distance ≤ radius + min(accuracy, 30 m))
                      no → if tenant allows "outside geofence with reason": ask reason, mark Flagged
                           else block
 → selfie (if required for employee/location): front camera, no gallery,
   on-device face detection: exactly 1 face + blink → else retake
 → build punch {clientId: UUIDv7, type: In/Out, deviceTime, elapsedSinceBoot,
                lat, lng, accuracy, isMock, geofenceId, selfieLocalPath, reason, deviceId}
 → save to local queue (drift) FIRST → show success immediately ("Synced" / "Pending sync")
 → sync engine uploads selfie (presigned) → POST /attendance/punches/batch (Idempotency-Key)
```
**Server-side validation (authoritative)** in the time service:
- Geofence recomputed server-side; employee must be assigned to the location (or "any location" policy).
- **Time integrity**: server compares `deviceTime` with server time estimated from `elapsedSinceBoot` delta since the last online sync; drift > 5 min ⇒ use server-derived time + Flag. Offline punches older than **72 h** (configurable) ⇒ Rejected/needs regularization.
- Duplicate In within 1 min ignored (idempotent); Out without In ⇒ accepted, day marked "missing punch".
- Mock location / rooted device (if tenant blocks) ⇒ Rejected; (if tenant warns) ⇒ Flagged.
- Optional **server face match** against profile photo (tenant opt-in; provider behind `IFaceMatchService`, data kept in India region) ⇒ low score Flagged.
- Punch status: **Valid / Flagged / Rejected**. Flagged punches go to HR review queue; they count as provisional until reviewed. Employee sees status + reason.
- Attendance day recomputed after each punch (status, late-in, early-out, work hours, OT).

#### B. Offline sync engine
- Queue table: `pending_punches`, `pending_uploads` with state `Pending → Uploading → Synced | Failed(retryable) | Rejected(final)`.
- Triggers: on connectivity regained, app resume, after each punch, periodic `workmanager` task (~15 min, OS permitting).
- Exponential backoff; selfie uploaded before punch; idempotent so re-sends are safe.
- Banner on Home: "2 punches waiting to sync". Logout with unsynced punches ⇒ warn and attempt sync first.
- **Cached read-only data** (drift, encrypted): profile, leave balances (with "as of" time), holidays, last 12 payslip summaries + downloaded PDFs, attendance of last 2 months, geofences & today's shift, directory. Stale-while-revalidate; cache wiped on logout / tenant switch.

#### C. Leave application
1. Pick type → app shows balance + policy hints (min notice, max days, attachment needed, half-day allowed).
2. Pick dates → server preview call `POST /leave-requests/preview` returns working days (excludes weekly offs/holidays per policy, sandwich rule), balance after, conflicts (team members already off), validation errors.
3. Submit (Idempotency-Key) → status Pending → workflow → push notification on each step → Approved/Rejected.
4. Cancel: allowed before start (no approval) or after start (requires approval per policy).

#### D. Regularization (missed / wrong punch)
Pick date from calendar (only past dates within allowed window, e.g. current payroll period not locked) → choose correct in/out times + reason → workflow → on approval the day is recomputed.

#### E. Expense claim
New claim → add items (camera receipt → compressed ≤ 1 MB, category, amount, date) → client shows policy limit warnings; server enforces limits by grade/category → save draft (online) or submit → workflow → Finance payout / payroll → status updates via push.

#### F. Approvals (Manager / HR)
Inbox shows tasks assigned to me (including delegated). Detail screen shows the context needed to decide (balance, team overlap, policy violations, receipts). Approve/reject/send back with comment; bulk approve for leave/regularization. Optimistic removal from list; undo for 5 s before sending.

#### G. Notifications & deep links
| Event | Recipient | Deep link |
|---|---|---|
| Approval task assigned / escalated | approver | `hrms://inbox/{taskId}` |
| Request approved / rejected / sent back | requester | `hrms://requests/{type}/{id}` |
| Payslip published | employee | `hrms://pay/payslips/{month}` |
| Missed punch (no check-out by shift end + 2 h) | employee | `hrms://attendance/regularize?date=` |
| Check-in reminder (shift start, not punched) — optional | employee | `hrms://attendance/punch` |
| Punch flagged / rejected | employee (+ HR review) | `hrms://attendance/day/{date}` |
| Declaration/proof window open/closing | employee | `hrms://pay/tax` |
| Document expiring, asset to acknowledge, announcement, birthday | employee | respective screen |
- Push payload carries only ids + generic text (no salary/PII on lock screen).
- User can mute categories; approvals and payroll can't be muted below "in-app".

### Privacy & compliance (India DPDP Act 2023)
- In-app **consent** for location and selfie/face data: purpose, retention (selfies 90 days default), withdrawal path; consent version stored server-side.
- Location collected **only at punch time** — never in background.
- Store listings: location "while in use" only; Google Play Data Safety + Apple privacy labels completed; camera/location usage strings explained.
- Screenshots blocked on payslip/bank screens (Android `FLAG_SECURE`, iOS blur on app switcher).

### Flutter project structure
```
hrms_mobile/
├─ lib/
│  ├─ main_dev.dart / main_staging.dart / main_prod.dart   # flavors
│  ├─ app/            # App widget, router, theme (Material 3, tenant primary colour, light/dark), l10n
│  ├─ core/           # auth, api (dio + interceptors), db (drift), sync engine, push, signalr,
│  │                  # permissions, connectivity, device integrity, errors, logging
│  ├─ shared/         # widgets (status chip, approval timeline, amount text ₹, empty/error/skeleton),
│  │                  # formatters (en-IN), validators
│  └─ features/
│     ├─ home/  attendance/  leave/  pay/  expense/  asset/  profile/
│     ├─ inbox/ team/  hr_lite/  notifications/  directory/  settings/  onboarding/
│     └─ <feature>/ data/ (repositories, local + remote sources, DTO→model mappers)
│                   domain/ (models, business rules)
│                   presentation/ (screens, widgets, riverpod providers/notifiers)
├─ packages/hrms_api/    # GENERATED — do not edit
├─ test/  integration_test/
└─ l10n/ (app_en.arb …)
```
Rules: no business logic in widgets; repositories return `Result`/sealed failure types; every screen handles loading / empty / error / offline states; all strings via ARB; `const` widgets; accessibility labels on icons; text scales to 200% without overflow.

### Testing (mobile)
Unit (business rules: geofence math, time integrity, leave preview mapping), widget + **golden** tests (light/dark), `integration_test` + **Patrol** for native permission dialogs / camera / location flows, mock API via generated client fakes; device matrix: low-end Android, recent Android, iPhone SE size, latest iPhone.

### Build & release
- Flavors: `dev`, `staging`, `prod` (separate bundle ids + Firebase projects + API URLs).
- CI: **Codemagic** or GitHub Actions + **Fastlane** → Play internal track / TestFlight on every `main` build; staged rollout to production (10% → 50% → 100%).
- Code signing secrets in CI vault; obfuscation (`--obfuscate --split-debug-info`) with symbols uploaded to Crashlytics.
- Versioning: semantic version + build number; **minimum supported version** enforced via `/me/bootstrap` → force-update screen.
- Optional later: Shorebird code push for urgent Dart-only fixes; white-label tenant builds.

## Stage 9 — Docker & Deployment ✅

### Decisions
| Topic | Decision |
|---|---|
| Cloud | **AWS Mumbai (`ap-south-1`)** — all data stays in India |
| CI/CD | **GitHub Actions** (monorepo on GitHub), AWS access via **OIDC** (no long-lived keys) |
| Registry | **Amazon ECR** (EC2 pulls via IAM instance role) |
| Environments | **Local** (Docker Compose) → **Staging** (auto-deploy from `main`) → **Prod** (manual approval) |
| Logging / monitoring | **Serilog → Seq** + health checks + external uptime monitor + basic CloudWatch alarms. OpenTelemetry tracing instrumented in code and exported to Seq (OTLP), so switching to Grafana later is config-only |
| Infra as code | **Terraform** for all AWS resources (VPC, ALB, EC2, RDS, S3, ECR, IAM, Route 53, ACM, SES) |

> Seq licence note: free "Individual" licence covers one user; a small paid licence is needed when several team members use Seq in prod.

### Production topology (AWS)
```
                 Route 53:  *.hrms.app, admin.hrms.app, api via same host
                                   │
                    ALB (ACM wildcard cert, TLS 1.2+, HTTP→HTTPS)
          ┌────────────────────────┼──────────────────────────────┐
   /api/*, /hubs/*, /connect/*, /account/*            everything else
          │                                                    │
  ┌───────▼──────────── EC2 "app" (Docker Compose) ────────────▼──────┐
  │ gateway(YARP) · identity · corehr · time · payroll · workflow     │
  │ expense · talent · web(nginx, Angular build)                      │
  └───────┬───────────────────────────────────────────────────────────┘
          │ private
  ┌───────▼──── EC2 "infra" (Docker Compose) ────┐     ┌──────────────────────┐
  │ RabbitMQ · Redis · Seq · ClamAV              │     │ RDS PostgreSQL 16     │
  └──────────────────────────────────────────────┘     │ Multi-AZ, encrypted,  │
                                                       │ PITR, private subnet  │
  S3 (files: versioned, SSE, private; backups bucket)   └──────────────────────┘
  SES (email) · MSG91 (SMS) · FCM/APNs (push)
```
- Start: **1 app VM + 1 infra VM** (e.g. `m7i.large`/`t3.large` class, gp3 disks); RDS starts small (`db.t4g.medium`) Multi-AZ once paying customers exist.
- **ALB** chosen (vs. nginx on VM) for managed TLS, health checks, and to add a 2nd app VM later with no DNS change.
- Security groups: app VM accepts only from ALB; infra VM only from app VM; RDS only from app VM. **No SSH** — access via **AWS SSM Session Manager**.
- Staging: **one VM** running everything incl. Postgres 16 + SeaweedFS S3 containers (cheaper); same images and compose files as prod.

### Containers
| Image | Base | Notes |
|---|---|---|
| Each .NET service + gateway | build: `mcr.microsoft.com/dotnet/sdk:8.0` → run: `mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled` | multi-stage, non-root, no shell, read-only root fs, `DOTNET_EnableDiagnostics=0` in prod |
| Each service **migrator** | same SDK build → runtime bundle (`dotnet ef migrations bundle`) | runs once per deploy, exits |
| web | build: `node:lts` (`ng build`) → run: `nginxinc/nginx-unprivileged` | SPA fallback, gzip/brotli, long cache for hashed assets, no-cache for `index.html`, security headers (CSP, HSTS, X-Frame-Options) |
| Infra | `rabbitmq:3-management`, `redis:7`, `datalust/seq`, `clamav/clamav` | pinned versions, named volumes |

- `.dockerignore` everywhere; images tagged with **git SHA** (+ `staging`/`prod` moving tags); **Trivy** scan in CI blocks HIGH/CRITICAL.
- Resource limits (`mem_limit`, `cpus`) per container; restart `unless-stopped`; Docker log rotation (`max-size 10m`, `max-file 3`).
- Startup order: migrators (`service_completed_successfully`) → services. Readiness via `/health/ready` checked by ALB + deploy script (chiseled images have no curl, so no in-container HEALTHCHECK).

### Local development
`deploy/docker-compose.yml` (base) + `docker-compose.override.yml` (dev) with **profiles**:
| Profile | Starts | Use |
|---|---|---|
| `infra` (default) | Postgres 16, RabbitMQ (mgmt UI :15672), Redis, SeaweedFS S3 (:8333, ADR 0002), Seq (:5341), **Mailpit** (fake SMTP, :8025), pgAdmin | Run services from IDE / `dotnet watch`, Angular via `ng serve`, Flutter on emulator |
| `full` | infra + all services + gateway + web | Full-stack test, demo, e2e |
| `tools` | ClamAV, migrators | When needed |

- One command: `docker compose --profile full up -d`; seed script creates a demo tenant (`demo.localhost`) with sample employees, policies and users for each role.
- `*.localhost` subdomains for tenant routing in dev; HTTPS via dev certs (`dotnet dev-certs`).
- `.env.example` documents every variable; real `.env` git-ignored.
- Mobile dev: emulator points to host (`10.0.2.2` Android) via the `dev` flavor.

### Compose files
```
deploy/
├─ docker-compose.yml            # services, networks, volumes (env-agnostic)
├─ docker-compose.override.yml   # local dev: ports, infra containers, hot paths
├─ docker-compose.staging.yml    # single-VM staging: postgres + seaweedfs containers, ECR images
├─ docker-compose.prod.app.yml   # prod app VM: ECR images, limits, no infra containers
├─ docker-compose.prod.infra.yml # prod infra VM: rabbitmq, redis, seq, clamav
├─ nginx/                        # web container config
├─ scripts/ deploy.sh, smoke-test.sh, rollback.sh, seed-demo.sh
└─ terraform/ (modules: network, compute, rds, s3, ecr, iam, dns, ses; envs: staging, prod)
```

### Configuration & secrets
- 12-factor: config via environment variables over `appsettings.json` defaults; `ASPNETCORE_ENVIRONMENT` = `Development|Staging|Production`.
- Secrets (DB passwords per service user, RabbitMQ creds, JWT signing/encryption certs, PII encryption keys, MSG91/SES/FCM keys) in **AWS SSM Parameter Store (SecureString, KMS)**; deploy script renders them into a root-only `.env` on the VM at deploy time. Never in git, images, or logs.
- GitHub **Environments** (`staging`, `prod`) hold only the AWS role ARN; `prod` requires reviewer approval.
- Key rotation: signing keys rolled via OpenIddict key set (old key kept for validation); PII keys versioned (key id stored with ciphertext).
- Feature flags: `Microsoft.FeatureManagement` with per-tenant filters (plan/module enablement) for gradual rollout.

### CI/CD pipelines (GitHub Actions)
| Workflow | Trigger | Steps |
|---|---|---|
| `ci-backend.yml` | PR / push, path filter per service + BuildingBlocks | restore → build → unit tests → integration tests (Testcontainers Postgres/RabbitMQ) → coverage gate → OpenAPI export + `oasdiff` breaking check |
| `ci-web.yml` | PR / push `src/Web/**` | npm ci → lint → unit tests → build (prod) → bundle budget check → Playwright e2e (against `full` compose) on `main` |
| `ci-mobile.yml` | PR / push `src/Mobile/**` | `flutter analyze` → tests → build APK/IPA (staging flavor) → Play internal / TestFlight on `main` (Codemagic or Fastlane) |
| `clients.yml` | API spec changed | regenerate TS + Dart clients → open PR |
| `security.yml` | PR + nightly | dependency audit (`dotnet list package --vulnerable`, `npm audit`), CodeQL, secret scanning, Trivy image scan |
| `deploy.yml` | push `main` → staging automatically; **manual approval** → prod | build & push changed images to ECR (tag SHA) → SSM Run Command on VM(s): pull → run migrators → `docker compose up -d` (changed services only) → wait `/health/ready` → smoke tests → notify |
| `terraform.yml` | PR on `deploy/terraform/**` | fmt, validate, plan (comment on PR) → apply on merge with approval |

### Deployment strategy
- **Migrations first**, always backward-compatible (expand → migrate → contract), so old containers keep working during the switch.
- Single app VM: services restarted one at a time (gateway retries + client retries cover the few-second gap); deploys scheduled outside India business hours for prod.
- When a **2nd app VM** is added: rolling deploy — deregister VM from ALB target group → update → health OK → re-register → next VM (zero downtime).
- **Rollback**: `rollback.sh <previous-sha>` re-deploys previous image tags (DB stays on new schema thanks to expand/contract). Last 10 image versions retained in ECR.
- Release versioning: semantic version tags (`v1.4.0`) on prod deploys; changelog generated from PR titles.

### Logging & monitoring
- **Serilog** structured JSON (CLEF) → **Seq**; every log enriched with `service`, `environment`, `tenantId`, `userId`, `correlationId`, `traceId`. **No PII / secrets / tokens** in logs (destructuring policies mask fields).
- **OpenTelemetry** tracing (ASP.NET Core, HttpClient, EF Core/Npgsql, MassTransit) exported via OTLP to Seq → follow one request across gateway → services → RabbitMQ.
- Seq retention: 30 days (prod), 7 days (staging); Seq **alerts** → email/Slack: error-rate spike, `_error` queue messages, payroll/import job failures, login-failure spike, health check failures.
- **Health checks**: `/health/live` and `/health/ready` (DB, RabbitMQ, Redis, S3) per service; ALB target health; deploy smoke tests.
- **External uptime monitor** (UptimeRobot / Better Stack free tier) on login page, `/api/v1/health` (gateway aggregate) and careers page → alerts SMS/email.
- **CloudWatch alarms** (built-in metrics, minimal cost): EC2 CPU/memory/disk (CloudWatch agent), RDS CPU, free storage, connections, replica lag; billing alarm.
- RabbitMQ management UI and Seq reachable only via SSM port-forwarding.

### Backups & DR
- RDS: automated backups + PITR (14 days prod), weekly manual snapshot copied to a second region; quarterly restore drill.
- S3: versioning + lifecycle (selfies expire 90 days; old versions → Glacier); cross-region replication for documents bucket.
- RabbitMQ and Redis: no backup needed (outbox in Postgres re-publishes; Redis is cache only).
- Full environment rebuild from Terraform + images + RDS snapshot: target **RTO ≤ 4 h, RPO ≤ 15 min** (matches Stage 4).

### Scaling path (when needed)
1. Vertical: bigger app VM / RDS class.
2. Horizontal: 2nd app VM behind ALB; Redis backplane for SignalR (already planned); Hangfire uses Postgres locks so jobs don't double-run.
3. Move hot services (time/payroll) to separate VMs, RDS read replica for reports.
4. Containers already 12-factor → migrate to **ECS Fargate** or **EKS** without code changes.

## Stage 10 — Testing, Security, Scalability ✅

### Decisions
| Topic | Decision |
|---|---|
| Year-1 scale target | **100 tenants / 25,000 employees** |
| Performance SLOs | API **p95 < 500 ms**, p99 < 1.5 s (excluding file upload/report export); check-in burst **~25k punches in 30 min** (≈ 15 req/s avg, 60 req/s peak) with p95 < 800 ms; payroll run **1,000 employees < 5 min**; web first load < 3 s on 4G; 99.5% monthly availability |
| Security assurance | **OWASP ASVS Level 2** + automated scans in CI + **external pentest** before first paying customer (then yearly and after major changes) |
| Payroll correctness | **Golden test cases** — hand-verified scenarios, every build must match **to the paisa** |

### Test strategy (pyramid)
| Layer | Backend (.NET) | Web (Angular) | Mobile (Flutter) |
|---|---|---|---|
| Unit | xUnit + FluentAssertions + NSubstitute — domain rules, handlers, validators, calculators | Vitest — stores, services, pipes, validators | `test/` — domain rules (geofence, time integrity), repositories, notifiers |
| Component / widget | — | Angular Testing Library — page + shared UI components | widget + **golden** tests (light/dark, text scale) |
| Integration | **Testcontainers** (Postgres 16 with RLS, RabbitMQ, Redis, SeaweedFS S3) + `WebApplicationFactory` — full API → DB round trips, consumers, outbox | — | repository ↔ fake API, drift DB |
| Contract | event contract tests (publisher schema vs consumer expectation); OpenAPI breaking-change check (`oasdiff`) | generated client compiles against spec | generated client compiles against spec |
| Architecture | **NetArchTest**: Domain has no infra refs, Application doesn't reference Api, no service references another service's projects | ESLint boundaries (features don't import each other's internals) | `custom_lint` import rules |
| End-to-end | — | **Playwright** against `full` compose: login → apply leave → manager approves → balance updated; payroll run happy path | **Patrol** on emulator: login, permission dialogs, punch (mocked GPS), offline → sync |
| Load | **k6** (see below) | — | — |

**Coverage gates** (CI fails below): Domain + Application projects **≥ 80% line coverage**; payroll calculation engine **≥ 95%**; no gate on Api/UI projects (covered by integration/e2e).

### Mandatory cross-cutting test suites
Built once as reusable test fixtures in `tests/Hrms.Testing` and applied to **every** service:
1. **Tenant isolation** — for every tenant-owned entity: data created as tenant A is invisible/unmodifiable as tenant B, via API (404) **and** directly via DB with RLS (0 rows).
2. **Data-scope leakage** — Manager sees only reports; Location-scoped HR sees only that location; Employee sees only Own.
3. **Permission matrix** — every endpoint is decorated (`[HasPermission]` or `[AllowAnonymous]` explicitly); test enumerates endpoints and fails on any undecorated one; role × endpoint expected 200/403 table.
4. **Field-level** — salary/bank/statutory fields absent without permission.
5. **Idempotency** — replayed `Idempotency-Key` returns same response, no duplicate rows.
6. **Concurrency** — stale `If-Match` → 412.
7. **Audit** — writes produce `audit_log` entries with masked sensitive fields.
8. **Messaging** — consumers are idempotent (same message twice = one effect).

### Payroll golden-test library
Location: `tests/Payroll.Golden/cases/<id>-<name>/` → `input.json` (tenant settings, state, employee, salary structure, attendance/LOP, declarations, loans, one-time items) + `expected.json` (every payslip line, statutory contributions, TDS, net pay). Each case reviewed and signed off by a qualified payroll/CA reviewer before it is added.

Minimum scenario set (Phase 1):
| Area | Cases |
|---|---|
| PF | basic below / above ₹15,000 ceiling; employer contribution split (EPS/EPF); VPF; PF on restricted vs actual wage; international worker excluded |
| ESI | gross ≤ ₹21,000 (covered); crossing threshold mid contribution-period (continues till period end); exempt |
| Professional Tax | Maharashtra (incl. February ₹300), Karnataka, Gujarat, Tamil Nadu (half-yearly), West Bengal; state with no PT |
| LWF | Maharashtra (June/December), Karnataka (annual) |
| TDS | new regime default; old regime with 80C/80D/HRA/home-loan interest; standard deduction; rebate u/s 87A edge; surcharge not applicable; mid-year joiner with previous employer income; regime switch at declaration window; proof shortfall in Jan–Mar recalculation |
| Attendance | LOP days (calendar vs working-day basis per tenant setting); mid-month joiner; mid-month leaver; unpaid weekly-off sandwich |
| Other | arrears from back-dated revision; loan EMI deduction; one-time bonus; reimbursement via payroll; negative net pay guard |
| F&F | notice-period shortfall recovery, leave encashment, gratuity (≥ 5 years, formula 15/26), pending loan recovery |

- Statutory rates live in **effective-dated data**, not code. Yearly update process: new Budget / state notification → update rate tables (new `effective_from`) → add/adjust golden cases → CA review → release before 1 April.
- Golden suite runs on every PR touching payroll; any diff fails the build with a line-by-line comparison report.

### Load testing (k6)
- Seed script creates 100 tenants with realistic size distribution (mostly 50–500 employees, a few ~1,000), 12 months of attendance, leave, and payroll history.
- Scenarios: (1) **morning punch rush** — 25k punch batches over 30 min with selfie upload; (2) **steady mixed load** — dashboards, lists, leave apply/approve at 3× expected daytime traffic; (3) **month-end** — 20 concurrent payroll runs + payslip publishing + attendance lock; (4) **soak** — 2 h steady to catch leaks.
- Run on staging sized like prod before every major release; results (p95, error rate, DB CPU, queue depth) stored in `docs/perf/`; regressions > 20% block release.

### Security programme
**ASVS L2 mapping** (how earlier stages satisfy it — checklist kept in `docs/security/asvs-l2.md`):
| ASVS area | Where handled |
|---|---|
| V2 Authentication, V3 Session | Stage 6 (OpenIddict, MFA, lockout, OTP limits, refresh rotation, session revocation) |
| V4 Access control | Stage 2 + Stage 6 (permissions, scopes, SoD, tests above) |
| V5 Validation / encoding | FluentValidation on all inputs, parameterised EF/Dapper only, Angular auto-escaping, CSP |
| V6 Cryptography | TLS everywhere, AES-256-GCM PII encryption, KMS-protected keys, key rotation |
| V7 Logging | Serilog with PII masking, security audit events, no secrets in logs |
| V8 Data protection | field-level hiding, `FLAG_SECURE`, no PII in push, retention rules (Stage 4) |
| V9 Communications | TLS 1.2+, HSTS, `sslmode=require`, internal services not exposed |
| V12 Files | presigned uploads, type/size allow-list, ClamAV scan, private buckets |
| V13 API | ProblemDetails without internals, rate limits, request size limits, CORS allow-list |
| V14 Configuration | chiseled non-root images, secrets in SSM, dependency/image scanning, security headers |

- **Threat model** (STRIDE) per service + data-flow diagram in `docs/security/threat-model/`, reviewed when a service's boundary changes. Top risks to design against: cross-tenant leakage, privilege escalation via role editing, salary data exposure, punch spoofing (GPS/time/selfie), payroll tampering (maker-checker + audit), account takeover via OTP abuse, malicious file upload.
- **CI security gates**: CodeQL (C#, TS, Dart where supported), `dotnet list package --vulnerable`, `npm audit --audit-level=high`, Trivy (images + IaC), GitHub secret scanning + push protection, **OWASP ZAP baseline** scan against staging after each deploy, `tfsec`/Checkov on Terraform.
- **Pentest scope**: web app (all roles), public APIs, mobile apps (Android + iOS, MASVS-L1), auth flows, multi-tenancy isolation, file uploads, AWS configuration review. Findings: critical/high fixed before go-live; medium within 30 days.
- **Incident response runbook** (`docs/runbooks/incident-response.md`): severity levels, on-call contact, containment steps (revoke sessions, rotate keys, block tenant), evidence preservation (Seq + audit logs + CloudTrail), customer communication templates.
- **DPDP Act 2023 obligations**: privacy notice + consent records (versioned); data-principal requests (access, correction, erasure where law allows — payroll records retained per tax law); **breach notification** to Data Protection Board and affected persons; Data Processing Agreement with each tenant (tenant = data fiduciary, platform = processor); sub-processor list (AWS, MSG91, Firebase, SES).
- Secure SDLC: PR review required, branch protection on `main`, dependency updates via Dependabot weekly, signed commits optional, security champion review for auth/payroll changes.

### Scalability levers (in order)
Covered in Stage 9 "Scaling path"; additionally: Redis caching of read-heavy lookups (permissions, bootstrap, holidays, policies), async processing for anything > 2 s (202 + jobs), DB indexes reviewed from `pg_stat_statements` monthly, connection pooling via RDS Proxy if connections become the bottleneck.

### Production-readiness checklist (go-live gate)
- [ ] All golden payroll cases pass; parallel check on one internal/demo payroll
- [ ] Tenant-isolation, permission-matrix and scope suites green for every service
- [ ] k6 scenarios meet SLOs on prod-sized staging
- [ ] Pentest done; no open critical/high findings
- [ ] ASVS L2 checklist complete with evidence links
- [ ] Backups verified by an actual restore; RPO/RTO drill recorded
- [ ] Alerts wired and tested (Seq, uptime monitor, CloudWatch, billing)
- [ ] Runbooks: deploy, rollback, incident response, restore, key rotation, tenant off-boarding
- [ ] Rate limits, request size limits, CORS, CSP, HSTS verified in prod config
- [ ] Data export (tenant self-service) and tenant deletion flows tested
- [ ] Legal: Terms of Service, Privacy Policy, DPA, cookie notice; SMS DLT templates approved; store listings approved
- [ ] Support process: help desk email, impersonation consent flow tested, status page

---

## Build Prompts ✅
Delivered as **`CLAUDE.md`** (standing coding rules) + the numbered prompt files in **`prompts/`**. Start with `prompts/00-HOW-TO-USE.md`.
