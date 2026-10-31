# 20 — Hardening & go-live

## Goal
Prove the system is secure, correct and fast enough for paying customers, and complete every item of the production-readiness checklist.

## Read first
`CLAUDE.md`; requirements → Stage 10 (all), Stage 6 (security controls), Stage 8 "Privacy & compliance", Stage 9 "Backups & DR".

Run in **separate sessions per part**.

## Part A — Security
- Fill `docs/security/asvs-l2.md`: every applicable ASVS L2 requirement → status + evidence (code link, test, config). Fix gaps.
- STRIDE threat model per service in `docs/security/threat-model/` with data-flow diagrams (Mermaid); verify mitigations for the top risks listed in Stage 10.
- Review: endpoint permission coverage report, IDOR checks (other tenant/other employee ids), mass-assignment (DTOs only), file upload abuse, OTP/login abuse, rate limits, CSP without `unsafe-inline` where possible, cookie/token storage, error leakage, logging of PII (scan Seq for patterns: PAN/Aadhaar/account numbers).
- Mobile: MASVS-L1 self-check (secure storage, TLS, no secrets in app bundle, obfuscation, `FLAG_SECURE`, root detection).
- Prepare pentest package: scope, test accounts per role across 2 tenants, staging URL, API spec; track findings in `docs/security/pentest-findings.md`.

## Part B — Correctness
- Golden payroll suite fully signed off (no `PENDING_CA_REVIEW`); one full parallel payroll on demo data reconciled.
- Cross-cutting suites (tenant isolation, scope, permission matrix, idempotency, concurrency, audit, messaging idempotency) green for all services — produce a coverage report per service.
- End-to-end Playwright + Patrol suites for: onboarding a tenant, hire → onboard, daily attendance, leave, payroll month, expense, exit → F&F.

## Part C — Performance
- k6 scripts in `tests/load/` for the four Stage 10 scenarios + seeding script for 100 tenants / 25k employees.
- Run on prod-sized staging; tune (indexes from `pg_stat_statements`, caching, batch sizes, connection pools); record results in `docs/perf/<date>.md` against SLOs.

## Part D — Operations & compliance
- Runbooks: deploy, rollback, incident response, restore, key rotation, tenant off-boarding/data export, impersonation support.
- DPDP: privacy notice, consent records verified, data-principal request handling, breach notification procedure, DPA template, sub-processor list.
- Tenant data export + deletion flows tested.
- Final walk through the **Production-readiness checklist** in Stage 10; every box ticked with evidence.

## Acceptance criteria
- No open critical/high security findings; ASVS L2 checklist complete.
- SLOs met (p95 < 500 ms API, punch burst p95 < 800 ms, payroll 1,000 employees < 5 min).
- All suites green in CI; restore drill and rollback rehearsed and documented.

## Verify
```bash
dotnet test
k6 run tests/load/punch-burst.js
cd src/Web/hrms-web && npx playwright test
cd src/Mobile/hrms_mobile && patrol test --flavor staging
```

## Ask me first
- Pentest vendor and timing; go-live date and first customer(s) for a pilot.
