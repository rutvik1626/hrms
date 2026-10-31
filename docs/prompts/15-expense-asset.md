# 15 — Expenses & Assets (service + web + mobile)

## Goal
Expense claims, advances and asset management end-to-end across backend, web and mobile.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Expenses", "Assets"; Stage 4 `hrms_expense` tables; Stage 7 "Screen inventory" (Expenses & Assets); Stage 8 "E. Expense claim", feature matrix rows for expenses/assets.

Run in **two sessions** (A: service, B: web + mobile).

### Part A — Service
- Employee projection; expense categories (receipt required above ₹X, mileage rate, per-diem), policies by grade/category (per-claim and monthly limits), claims (draft → submitted → workflow → approved → paid/reimbursed via payroll), items with receipts (presigned upload, scan), duplicate receipt detection (amount + date + merchant hash), policy violation flags shown to approvers.
- Advances (request → approve → paid → settle against claims; outstanding recovery hook for F&F).
- Payout: Finance marks paid (direct) or routes to payroll (`ExpenseApprovedForPayroll`).
- Assets: categories, assets (serial, purchase date/cost, warranty, status), allocation/return with employee acknowledgement, damage/loss, asset requests via workflow, `EmployeeSeparated` → recovery checklist items + events for exit clearance/F&F.
- Reports: claims by status/category/department, asset register.

### Part B — Web + mobile
- Web: my claims, new claim (multi-item), claim detail + timeline, Finance payout queue, advances, expense policy settings; asset catalog, asset detail (history), allocate/return, my assets, asset requests; inbox detail components for expense/asset approvals.
- Mobile: claims list, new claim with **camera receipt** (compress ≤ 1 MB), drafts, status + timeline, advances; my assets + acknowledge, request asset; deep links.

## Acceptance criteria
- Claim over policy limit is flagged (or blocked per policy) server-side; approver sees violations.
- Approved claim routed to payroll appears as reimbursement line in next payroll run (integration test across services in compose).
- Separation creates asset recovery items; unreturned asset visible in F&F.
- Standard suites + Playwright (claim → approve → payout) + Flutter tests green.

## Verify
```bash
dotnet test tests/Expense.UnitTests tests/Expense.IntegrationTests
cd src/Web/hrms-web && npx playwright test expense
cd src/Mobile/hrms_mobile && flutter test
```

## Ask me first
- Default expense categories and grade limits to seed.
