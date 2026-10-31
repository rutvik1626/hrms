# 03 — Identity & Tenant service

## Goal
Build the identity service: tenants and plans, users, roles/permissions/scopes, the OpenIddict token server with tenant-branded hosted login (password, SMS OTP, Google/Microsoft SSO, MFA), invites, devices, sessions and audited impersonation.

## Read first
`CLAUDE.md`; requirements → Stage 0, Stage 2 (all), Stage 4 "Key entities per database" (`hrms_identity`), Stage 5 "Mobile-specific endpoints" (`/me/devices`), Stage 6 (all).

Run in **two sessions**:

### Part A — Tenancy, users, RBAC
- Entities + migrations: tenants (subdomain, status, settings: logo, primary colour, time zone, financial year, locale), plans, subscriptions, tenant_modules, users (per-tenant unique email/phone/employee code), roles (system + custom), permissions (seeded catalogue), role_permissions, user_role_assignments (role + scope type + scope value), devices, impersonation_sessions, audit_log.
- **Permission catalogue** seeded from a single C# source (`Permissions.cs`, `<Module>.<Resource>.<Action>`) covering all Phase 1 modules; system roles seeded with defaults per Stage 2 table.
- APIs: tenant sign-up (public, creates tenant + Tenant Admin user, publishes `TenantCreated`), tenant settings, users CRUD, roles CRUD (custom roles; role admins cannot grant permissions they lack), role assignments with scope, `/me/permissions` (effective permissions + scopes), `/me/sessions`, `/me/devices`.
- Permission cache: publish `RoleAssignmentChanged`, bump `perm_ver`, write effective permission set to Redis; `/internal/users/{id}/permissions` fallback.
- Consumers: `EmployeeCreated` → create user + send invite; `EmployeeSeparated` → disable user, revoke sessions/devices (alumni option).
- Implicit Manager permissions for users with direct reports (from corehr events projection).

### Part B — OpenIddict + hosted login
- OpenIddict server: authorization code + PKCE, refresh token rotation with reuse detection, client credentials (service clients), token exchange (impersonation), signing/encryption keys from config (dev certs locally), access token claims per Stage 6.
- Clients registered: `hrms-web` (SPA), `hrms-mobile` (native, custom scheme redirect), one confidential client per service, biometric-agent client template.
- **Hosted login pages** (Razor Pages, tenant-branded via subdomain or `tenant` param): login (email/username/employee code + password), mobile OTP (send/verify via `ISmsSender` → MSG91 implementation + console fake for dev), Google + Microsoft external login (per-tenant config, link to existing user only), MFA (TOTP enrol/verify, email OTP, recovery codes), forgot/reset password, accept invite / set password, consent-free first-party flow, lockout + CAPTCHA hook.
- Step-up MFA: `acr`/`auth_time` support so APIs can demand recent MFA.
- Impersonation: tenant admin approval request → time-boxed token exchange (`act` claim, read-only scope default), audit events.
- Security audit events per Stage 6 list.

## Out
Gateway (04), UI for user/role management screens (06/18 — APIs only here).

## Acceptance criteria
- New tenant sign-up → admin logs in via code+PKCE from a test SPA client and gets tokens with `tid`, `sid`, `perm_ver`.
- Password, OTP (fake sender), Google/Microsoft (configurable, test with dev credentials), TOTP MFA flows all work; lockout after 5 failures; OTP rate limits enforced.
- Refresh token reuse revokes the session; device revoke kills its refresh tokens.
- Role assignment change reflected in `/me/permissions` and Redis within seconds.
- Tenant isolation + permission matrix suites green.

## Verify
```bash
dotnet test tests/Identity.UnitTests tests/Identity.IntegrationTests
```
Manual: open `http://demo.localhost:<port>/connect/authorize?...` with a test client (provide a `.http` file with the full flow).

## Ask me first
- SMS provider account details (MSG91 sender id / DLT template ids) — use the fake sender until provided.
- Tenant subdomain base domain for dev/staging/prod.
- Whether alumni (ex-employee) login is enabled by default.
