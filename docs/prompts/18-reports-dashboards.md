# 18 — Reports & role dashboards

## Goal
Finish every role dashboard with real data and deliver the standard report catalogue with filters, preview and export — without cross-database joins.

## Read first
`CLAUDE.md`; requirements → Stage 7 "Dashboards", "Screen inventory" (Reports line); Stage 4 "Performance & scale" (read replica, no cross-DB joins); Stage 5 "Bulk import / export".

## Scope
**In**
- Each service exposes its own report endpoints (Dapper read models, scoped by permission + data scope, paged preview + async xlsx/pdf export job).
- Report catalogue (minimum):
  - Core HR: headcount by dept/location/grade, joiners/leavers, attrition %, probation due, document expiry, employee master export.
  - Time: attendance register, late/early report, overtime, absenteeism, leave balance, leave availed, flagged punches.
  - Payroll: payroll register, variance, CTC, PF/ESI/PT/LWF summaries, TDS summary, bank statement, F&F.
  - Expense/Asset: claims by status/category/department, advances outstanding, asset register, allocation history.
  - Talent: requisition status, pipeline funnel, time-to-hire, offer acceptance rate.
- Dashboard widget endpoints per role (Stage 7 list), cached in Redis (1–5 min), with SignalR refresh where useful.
- Web: reports catalogue page (search, favourites), report viewer (filters, preview table, export), dashboards wired for Employee, Manager, HR Admin, Payroll Admin, Finance, Recruiter, Tenant Admin; mobile HR/Manager KPI cards reuse widget endpoints.
- Export permission (`*.Export`) enforced; exports audited.

**Out** — custom report builder / BI (future).

## Acceptance criteria
- Each report respects scope (Location HR sees only their location) — covered by integration tests.
- Exports of 25k rows complete as async jobs < 60 s; preview p95 < 1 s.
- Dashboards render with skeletons and no N+1 calls (single widget endpoint per dashboard section).

## Verify
```bash
dotnet test --filter Category=Reports
cd src/Web/hrms-web && npx playwright test reports
```

## Ask me first
- Any customer-specific must-have reports for launch.
