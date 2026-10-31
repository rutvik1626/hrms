# 16 — Talent: Recruitment, Onboarding & Exit

## Goal
Recruitment from requisition to hire, a public careers page, onboarding checklists that create employees in Core HR, and the exit process feeding F&F.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Recruitment & Onboarding / Exit"; Stage 4 `hrms_talent` tables; Stage 7 "Screen inventory" (Talent line); Stage 3 events (`CandidateHired`, `ExitClearanceCompleted`).

Run in **two sessions** (A: service, B: web; mobile parts are in 17).

### Part A — Service
- Requisitions (position, department, location, budget, count) → workflow; job openings (publish to careers page, internal-only flag).
- **Public careers page API** (tenant from subdomain, anonymous, rate-limited, CAPTCHA hook) + apply with resume upload (scan), consent text (DPDP).
- Candidates (dedupe by email/phone), applications, configurable pipeline stages per tenant, stage moves with audit, interviews (schedule panel, calendar invite email `.ics`), feedback scorecards (interviewer role sees only assigned), offers (templates with merge fields, CTC breakup from payroll structure preview, approval workflow, PDF, e-acceptance link with expiry).
- Pre-boarding portal (token link): candidate uploads documents and fills personal details.
- Hire → `CandidateHired` → corehr creates employee → onboarding checklist instantiated from template (tasks per department/assignee with due dates, notifications).
- Exit: resignation (from employee) → workflow → notice period/LWD calculation → exit checklist (IT, Admin, Finance, assets from expense service) → exit interview form → `ExitClearanceCompleted` → payroll F&F.
- Candidate data retention/purge job (configurable, e.g. 12 months for rejected).

### Part B — Web
- Requisitions, job openings, careers page (public Angular route or lightweight separate page, tenant-branded, SEO-friendly basics), candidate pipeline **Kanban** (drag between stages), candidate profile, interview scheduler + feedback form, offer builder + approval, onboarding checklist board, exit requests, clearance board, exit interview; hiring-manager view (own requisitions only).

## Acceptance criteria
- Anonymous apply on careers page → candidate in pipeline → interview → offer approved → accepted → employee created in corehr → onboarding tasks assigned → user invited (end-to-end in compose).
- Resignation → approvals → clearance completes → F&F created in payroll.
- Interviewer can't see other candidates or salary details; standard suites green; Playwright for hire flow.

## Verify
```bash
dotnet test tests/Talent.UnitTests tests/Talent.IntegrationTests
cd src/Web/hrms-web && npx playwright test talent
```

## Ask me first
- Default pipeline stages and offer letter template content.
