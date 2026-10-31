# 08 — Core HR service

## Goal
Implement organisation structure and the employee master with lifecycle, documents, profile change requests and bulk import, publishing the events every other service depends on.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Core HR", Stage 2 "Data scopes", Stage 4 `hrms_corehr` tables + "Sensitive data (PII)" + effective-dating note, Stage 3 Services table (corehr events), Stage 5 "Bulk import / export", "Files".

## Scope
**In**
- Org masters: legal entities (PAN/TAN/PF/ESI registration nos.), locations (with **state** for PT/LWF/holidays, address, geo point), departments (tree), designations, grades, cost centers. CRUD + soft delete + "in use" guards.
- Employee aggregate: employee code (tenant-configurable pattern), personal, contact, addresses, job (entity, location, department, designation, grade, cost center, employment type, joining date, probation end, notice period), **reporting manager**, bank accounts (encrypted, HMAC search), statutory (PAN, Aadhaar masked + encrypted, UAN, ESIC no., PF applicable flags), family/nominees, education, experience, photo, custom fields (JSON per tenant definitions).
- **Effective-dated assignments** (department, designation, location, grade, manager) with history; queries "as of date".
- Lifecycle actions (each a command + event + audit): confirm probation, transfer, promote, change manager, separation (resignation date, last working day, reason) — `EmployeeSeparated` published on effective date (Hangfire).
- Hierarchy queries: direct reports, all reports (recursive CTE), org chart API; `IDataScopeResolver` implementation for corehr scopes; **hierarchy projection events** for other services.
- Documents: types (with expiry), upload via presigned flow, expiry alerts job (events for notifications).
- Profile change requests (employee self-service edits for address/phone/bank/family) → `WorkflowRequested` → applied on `WorkflowCompleted`; bank change requires step-up MFA.
- `/me` profile endpoints; directory endpoint (limited public fields).
- Bulk import (employees, org masters) — Excel template download, async validation job, row-level error report, commit; export list to xlsx.
- Consumers: `TenantCreated` → seed default masters; `CandidateHired` (talent) → create employee draft.
- Events: `EmployeeCreated/Updated`, `EmployeeTransferred`, `ReportingManagerChanged`, `EmployeeSeparated`, `EmployeeConfirmed`, `DocumentExpiring` — minimal payloads.
- Field-level response shaping: bank/statutory hidden without permission.

**Out** — salary data (payroll), web screens (09).

## Acceptance criteria
- Create employee → identity user created + invite sent (end-to-end in compose).
- Manager scope returns only reports (recursive); Location-scoped HR sees only their location.
- Back-dated transfer shows correct department "as of" past and current dates.
- Import of 1,000 rows validates in < 30 s with a downloadable error report.
- Aadhaar never returned unmasked; audit shows `***changed***` for bank account.
- Standard suites (tenant isolation, permission matrix, idempotency, concurrency, audit) green.

## Verify
```bash
dotnet test tests/CoreHr.UnitTests tests/CoreHr.IntegrationTests
```

## Ask me first
- Mandatory vs optional employee fields for your target customers.
- Employee code pattern default (e.g. `EMP-0001`).
