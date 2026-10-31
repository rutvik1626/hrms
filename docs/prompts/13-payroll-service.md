# 13 — Payroll service (India)

## Goal
Implement India-compliant payroll: components and structures, effective-dated salaries, the monthly payroll run, PF/ESI/PT/LWF/TDS, declarations and proofs, loans, arrears, F&F, payslips, bank file and statutory reports — verified by the golden test library.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Payroll (India)", Stage 4 `hrms_payroll` tables + retention, Stage 5 "Concurrency, idempotency, async work", Stage 6 "Step-up MFA", "Segregation of duties" (maker–checker), Stage 10 "Payroll golden-test library".

**Rule zero:** statutory rates, ceilings and slabs are **effective-dated data**, never constants in code. Calculation engine is pure (no I/O) and deterministic; rounding rules are explicit and configurable per component.

Run in **three sessions**:

### Part A — Setup & calculation engine
- Projections: employee (from corehr), attendance month summaries (from `AttendanceMonthLocked`), approved reimbursements (from expense, later).
- Salary components (earning/deduction/employer contribution/reimbursement; fixed, formula, variable input; taxable/exempt flags; PF/ESI wage inclusion flags; pro-ration flag), formula language (safe expression evaluator, references other components, CTC, attendance variables).
- Salary structures/templates by grade; employee salary assignment (CTC → breakup), revisions (effective-dated; back-dated → arrears).
- Statutory settings: PF (rates, wage ceiling, employer split EPS/EPF/EDLI/admin, VPF, restrict-to-ceiling option), ESI (rates, threshold, contribution periods), PT slabs per state (monthly + special months), LWF per state, gratuity, income-tax slabs per regime/FY, standard deduction, rebate 87A, cess, surcharge (data-driven).
- **Calculation engine** producing payslip lines with explanations ("why this amount") — pure functions + unit tests.
- **Golden test harness** `tests/Payroll.Golden` (input.json / expected.json, line-by-line diff report) with the Stage 10 scenario set (values marked `PENDING_CA_REVIEW` until signed off).

### Part B — Payroll run & tax
- Pay groups (legal entity + frequency); payroll run lifecycle: **Draft → Inputs (attendance locked? one-time items, arrears, loans) → Computed → Reviewed (variance vs last month) → Locked (step-up MFA, maker ≠ checker if enabled) → Published** (payslips visible, `PayrollPublished`); re-compute individual employees before lock; run as background job with progress events (202 + job status).
- One-time payments/deductions, loans & advances with EMI schedule, holds (stop salary for an employee).
- TDS: regime choice window, investment declarations (80C, 80D, HRA with rent/landlord PAN rule, home loan, others), proof submission + verification by payroll, monthly TDS projection with true-up in Jan–Mar, previous employer income.
- Payslip PDF (QuestPDF, tenant branding, password-protect optional) stored + presigned access; YTD summary.

### Part C — Outputs & F&F
- Bank transfer file (generic CSV + configurable format per bank), payment status.
- Statutory reports: PF ECR file, ESI contribution file, PT summary by state, LWF, TDS 24Q data export, **Form 16** Part B generation (Part A from TRACES uploaded), payroll register, variance report, CTC report.
- Full & Final settlement on `EmployeeSeparated`: last month pro-rata, leave encashment (from time balance via internal API), notice recovery/pay, gratuity, loan recovery, asset recovery deduction hook → F&F statement → workflow → paid via off-cycle run.
- Expense reimbursement inclusion from `ExpenseApprovedForPayroll`.

## Acceptance criteria
- All golden cases pass to the paisa; engine ≥ 95% coverage; adding a new FY tax slab requires data only.
- Payroll run of 1,000 employees completes < 5 min with progress updates.
- Locked run is immutable; corrections only via arrears/off-cycle; all actions audited.
- Salary data never visible without `Payroll.*` / `Employee.Salary.View`; permission matrix + tenant isolation suites green.

## Verify
```bash
dotnet test tests/Payroll.UnitTests tests/Payroll.Golden tests/Payroll.IntegrationTests
```

## Ask me first
- **Every statutory number** and scenario expected value must be confirmed by you/your CA before golden cases leave `PENDING_CA_REVIEW`.
- Supported banks for bank files in v1; whether payslip PDFs are password-protected (e.g. PAN-based).
