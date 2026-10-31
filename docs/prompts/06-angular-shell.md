# 06 — Angular shell, theming & shared UI

## Goal
Build the web application foundation every feature plugs into: auth, API client, layout, theming, i18n, permissions, real-time, and the reusable page patterns — plus the platform admin screens (users, roles, workflows, tenant settings, setup wizard).

## Read first
`CLAUDE.md`; requirements → Stage 7 (all), Stage 5 "Conventions", "Error format", "Concurrency, idempotency, async work", "Files", Stage 6 "Login flow", "Tenant resolution at login", Stage 2 "Enforcement points".

Run in **two sessions**:

### Part A — Core & shell
- Generated API client setup (`ng-openapi-gen` from the gateway spec; npm script `api:generate`).
- `angular-auth-oidc-client` (code + PKCE, silent renew, tenant from subdomain), auth guard, logout everywhere.
- HTTP interceptors: base URL, `Idempotency-Key` (for marked requests), `If-Match` from stored ETags, correlation id, ProblemDetails → field errors / toast, 401 → re-login, 403 → access-denied page, offline detection.
- Bootstrap store (`/me/bootstrap`): user, permissions, modules, tenant branding → applies primary palette and logo; light/dark toggle (`.app-dark`), density setting.
- PrimeNG Aura preset + Bootstrap grid/utilities only (no reboot/JS), CSS layer order, global styles.
- Transloco (scoped per feature, `en` only now), `en-IN` formatting pipes (`inrCurrency`, `hrmsDate`, `hrmsDateTime`).
- Layout shell: collapsible sidebar (menu from route config with `permission` + `module`), top bar (Ctrl+K global search, Inbox badge, notifications bell, theme toggle, profile menu), breadcrumb, impersonation/plan banners, responsive drawer < 992 px.
- `*hasPermission` directive, `canMatch` permission guard, module guard.
- SignalR service (connect after login, reconnect, exposes signals for task count / notifications / job progress).
- Notifications panel + page; Inbox page shell (lists `/me/tasks`; detail opens module-specific component via registry).

### Part B — Shared UI patterns + platform admin screens
- `shared/ui`: page header, list page (lazy `p-table`, toolbar, filters drawer, saved filters, column chooser, export, bulk actions, empty/skeleton), detail page (header card + tabs + **History** tab using audit API), form helpers (typed reactive forms, server-error mapping, unsaved-changes guard, sticky save bar), wizard (`p-stepper`, resumable), approval timeline, employee picker (scoped autocomplete), date-range picker, amount input (₹), file uploader (presigned flow + scan status), confirm dialog, status tag, import flow component, job-progress component.
- Platform screens: tenant sign-up page, **setup wizard** (company → locations → departments → policies placeholder → import employees placeholder → invite), company settings (branding, time zone, FY, formats), users, roles & **permission matrix editor**, role assignment with scope, workflow definitions editor (step list + conditions), notification templates, security log, my sessions/devices, super-admin portal (tenants, plans, impersonation approvals) under `platform-admin/`.
- Role dashboards: layout + widgets slots (data wired in later prompts).

## Acceptance criteria
- Login via hosted page returns to app; menu shows only permitted items; deep link to unpermitted route → access denied.
- Tenant colour/logo and dark mode apply instantly; no Bootstrap component styles leak.
- Shared list page demonstrated with users list (server paging/sort/filter); History tab shows audit entries.
- Playwright: sign-up → setup wizard → create role with custom permissions → assign to user with Location scope → that user sees limited menu.
- Lint, unit tests, build budgets pass.

## Verify
```bash
cd src/Web/hrms-web
npm run api:generate && npm run lint && npm test && npx ng build
npx playwright test
```

## Ask me first
- Default brand colour and product logo for the platform (before tenants customise).
