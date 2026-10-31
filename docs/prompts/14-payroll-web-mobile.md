# 14 — Payroll web screens + mobile Pay tab

## Goal
Payroll admin screens on the web and employee pay/tax self-service on web and mobile.

## Read first
`CLAUDE.md`; requirements → Stage 7 "Screen inventory" (Payroll line), "Dashboards" (Payroll Admin); Stage 8 "Feature matrix" (payslips, tax), "Screen inventory" item 5, "Privacy & compliance" (`FLAG_SECURE`).

## Scope
**Web (Payroll Admin / Finance)**
- Salary components (with formula editor + test evaluator), structures, employee salary assignment & revision (CTC breakup preview, effective date, arrears preview), pay groups.
- Statutory settings pages (PF, ESI, PT/LWF by state, tax slabs) — read-only defaults with tenant overrides where allowed.
- **Payroll run wizard** (stepper per lifecycle, inputs checklist incl. attendance lock status, variance review table with drill-down "why this amount", lock with step-up MFA, publish), job progress live.
- One-time items, loans, holds, declarations window settings, **proof verification** queue, F&F screen, bank file download, statutory reports page.
- Payroll Admin dashboard: cycle status stepper, statutory due dates, pending proofs.

**Web + mobile (Employee)**
- Payslip list + detail (earnings/deductions, employer contributions) + PDF download; YTD; Form 16 download.
- Tax: regime choice (window-bound, with comparison estimate), investment declaration form, proof upload (mobile: camera), status.
- Mobile Pay tab per Stage 8 navigation; payslip/bank screens use `FLAG_SECURE`; payslip PDFs cached for offline viewing.

## Acceptance criteria
- Payroll Admin completes a full run for the demo tenant in the wizard; employee sees payslip on web and mobile after publish (push received).
- Users without salary permission never receive salary fields (verify network responses) and never see the menu.
- Playwright: payroll run happy path. Flutter widget/golden tests for payslip screens.

## Verify
```bash
cd src/Web/hrms-web && npm run api:generate && npm test && npx playwright test payroll
cd src/Mobile/hrms_mobile && flutter test
```

## Ask me first
- Whether employees can see employer contributions and CTC on the payslip (tenant setting default).
