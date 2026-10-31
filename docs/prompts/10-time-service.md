# 10 — Time service (Attendance + Leave)

## Goal
Implement shifts, punches from all capture modes with server-side validation, daily attendance processing, the leave engine, holidays, comp-off and the monthly attendance lock that feeds payroll.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Attendance", "Leave"; Stage 4 `hrms_time` tables, retention (selfies); Stage 5 "Mobile-specific endpoints" (`/attendance/punches/batch`); Stage 8 "A. Check-in / check-out" (server-side validation block), "C. Leave application", "D. Regularization"; Stage 10 load SLOs (punch burst).

Run in **three sessions**:

### Part A — Setup & punches
- Employee projection from corehr events (location, manager, grade, gender, joining/separation dates).
- Shifts (start/end, grace in/out, half-day/absent thresholds, night shift crossing midnight, breaks), weekly-off patterns (incl. alternate Saturdays), rosters (assign shift per employee per date, bulk/rotation), attendance policies per location/grade (capture modes allowed, selfie required, geofence required, outside-geofence behaviour, IP allow-list for web, offline window hours).
- Geofences (circle: centre + radius; multiple per location), employee ↔ location/geofence assignment.
- Punch ingestion: web punch (IP check), mobile batch (validation per Stage 8: geofence, time integrity with `elapsedSinceBoot`, offline window, mock/root, duplicate window, selfie file reference) → status **Valid/Flagged/Rejected** + reasons; optional face-match via `IFaceMatchService` (no-op default); HR review of flagged punches (approve/reject).
- Biometric devices: device registry per location; ADMS push endpoint (ZKTeco-compatible) + pull-agent API (client credentials) mapping device user id → employee; dedupe.
- Optimised for the burst: batch insert, minimal synchronous work, attendance recompute queued.

### Part B — Attendance processing & regularization
- Daily attendance computation per employee: status (Present, Absent, Half-day, Weekly-off, Holiday, Leave, On-duty), first-in/last-out, work hours, late-in/early-out minutes, overtime per policy; recompute on punch, leave approval, roster/holiday change; nightly job for absentees.
- Regularization requests (missed/wrong punch) → workflow → recompute; window limited to unlocked periods.
- **Month lock**: HR locks a month per legal entity/location after review → `AttendanceMonthLocked` with per-employee summary (payable days, LOP days, OT hours, late counts) for payroll; unlock (permission + audit) only if payroll not locked.
- APIs for my/team/register views (calendar month, day detail with punches + selfie presigned URLs), exceptions lists (late, missing punch, flagged).
- Missed-punch / check-in reminder events for notifications.

### Part C — Leave
- Holiday calendars per location/state, optional holidays (pick N).
- Leave types + policies (Stage 1 list: accrual monthly/yearly/pro-rata on joining, carry-forward + cap, encashment, lapse, max balance, negative balance, sandwich rule, min notice, min/max days, half-day/hourly, attachment after N days, applicability by gender/grade/location/employment type, probation restrictions).
- **Leave ledger** (append-only: accrual, availed, cancelled, adjusted, encashed, lapsed, carry-forward) → balances derived; scheduled accrual + year-end jobs (Hangfire, idempotent per period).
- `POST /leave-requests/preview` (working days, balance after, conflicts in team, validation), apply/cancel/withdraw via workflow; approval → ledger + attendance recompute; `LeaveApproved/Cancelled` events.
- Comp-off: credit request for working on WO/holiday (validated against attendance) → workflow → credit with expiry; avail as leave type.
- HR adjustments (with reason, audit); opening balance import.

## Acceptance criteria
- Punch validation unit tests cover every rule incl. clock tampering and offline window; burst test: 25k punches/30 min locally scaled scenario meets p95 < 800 ms on dev hardware equivalent (record numbers).
- Night shift crossing midnight computes correctly; sandwich rule and half-day cases covered by tests.
- Leave ledger reconciles: balance = sum(ledger) for every employee after accrual/year-end jobs run twice (idempotent).
- Month lock event contains correct payable/LOP days for test fixtures.
- Standard suites green.

## Verify
```bash
dotnet test tests/Time.UnitTests tests/Time.IntegrationTests
```

## Ask me first
- Default leave types/policies to seed for a new Indian tenant (proposal: CL 12, SL 12, EL/PL 15 accrual monthly, ML 26 weeks, PL (paternity) 5, LOP, Comp-off).
- Whether biometric integration targets ZKTeco ADMS push first or a Windows pull agent.
