# 09 — Core HR web screens

## Goal
Deliver the Core HR screens in Angular using the shared page patterns, plus the employee self-service profile.

## Read first
`CLAUDE.md`; requirements → Stage 7 "Screen inventory" (Core HR line), "Standard page patterns", "Navigation by persona", "Dashboards" (HR Admin widgets), "Formatting & localisation"; Stage 2 "System roles".

## Scope
**In**
- Regenerate API client.
- `features/core-hr/`: org masters pages (legal entities, locations with map pin, departments tree, designations, grades, cost centers); employee list (filters: department, location, status, employment type, joining date range; saved filters; export); **add employee wizard**; **employee 360° profile** (tabs per Stage 7 — permission-gated tabs hidden entirely); lifecycle action dialogs (confirm, transfer, promote, change manager, separate) with effective date; org chart (PrimeNG OrganizationChart, lazy expand); profile change requests queue; document expiry tracker; bulk import (shared import component).
- `features/me/profile`: view + request changes (with approval timeline), my documents.
- Directory page (search by name/department/location).
- Wire setup wizard steps (locations, departments, import employees) from 06 to real APIs.
- HR Admin dashboard widgets: headcount, joiners/leavers trend, probation due, document expiries, pending profile changes.
- Transloco keys for all text; History tab on employee and masters.

## Acceptance criteria
- Employee as Employee role: sees only own profile, no salary/bank edit fields, can submit change request.
- HR (Location scope) sees only that location's employees in list, search, org chart and directory.
- Wizard validation mirrors server errors (field-mapped ProblemDetails).
- Playwright: HR creates employee → employee receives invite (Mailpit) → logs in → requests address change → HR approves → profile updated.

## Verify
```bash
cd src/Web/hrms-web && npm run api:generate && npm run lint && npm test && npx playwright test core-hr
```

## Ask me first
- Which profile fields employees may edit directly without approval (default: none — all via request).
