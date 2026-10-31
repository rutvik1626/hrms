# 07 — Flutter app foundation

## Goal
Build the mobile app foundation: flavors, login, app lock, bootstrap, role-aware navigation, encrypted local DB, offline sync engine skeleton, push and deep links — so module prompts only add screens and repositories.

## Read first
`CLAUDE.md`; requirements → Stage 8 (all, especially "Tech stack", "Navigation", "Screen inventory" items 1, 2, 12, "B. Offline sync engine", "G. Notifications & deep links", "Privacy & compliance", "Flutter project structure"), Stage 6 "Mobile session & app lock", "Tenant resolution at login".

## Scope
**In**
- Flavors `dev/staging/prod` (bundle ids, app names, API base URLs, Firebase config placeholders); Android + iOS native setup (permissions strings, URL scheme `hrms://` + universal/app links stub).
- Generated Dart client (`packages/hrms_api`, `openapi-generator dart-dio`, script `tool/generate_api.sh`); dio interceptors: auth (refresh rotation, single-flight refresh), Idempotency-Key, correlation id, ProblemDetails → typed failures, connectivity.
- Auth: company-code screen (lookup tenant), `flutter_appauth` login, token storage in `flutter_secure_storage`, logout (+ sync guard), session expiry handling.
- **App lock**: set biometric or PIN after first login; lock on cold start + after 5 min background; 5 wrong PINs → wipe tokens; change PIN in settings.
- Permissions primer screens (location, camera, notifications) with rationale + DPDP consent capture (versioned, sent to server).
- Bootstrap: `/me/bootstrap` provider; force-update + maintenance screens; tenant primary colour → Material 3 theme; light/dark/system.
- go_router with auth + permission redirects; **role-aware bottom navigation** per Stage 8 table; placeholder screens for each tab.
- drift DB (SQLCipher, key in secure storage): tables for `pending_punches`, `pending_uploads`, cache tables framework (`cache_entries` with key, json, fetched_at); wiped on logout / tenant switch.
- **Sync engine skeleton**: queue processor with states, exponential backoff, triggers (connectivity, resume, workmanager periodic), presigned upload step, batch POST with idempotency; Home banner showing pending count. (Punch specifics come in 12.)
- Push: `firebase_messaging` + local notifications, token registration via `/me/devices`, deep-link handling from tap (cold + warm start).
- SignalR (foreground) for `taskCountChanged` / `notificationReceived`.
- Common screens: notifications centre, settings (language placeholder, theme, notification prefs), my sessions/devices, about/help, directory (stub repository).
- Shared widgets: status chip, approval timeline, ₹ amount text, empty/error/offline/skeleton states; `en-IN` formatters; ARB setup.
- Root/jailbreak detection (warn/block per tenant setting), `FLAG_SECURE` helper for sensitive screens.

**Out** — module screens (12, 14, 15, 17).

## Acceptance criteria
- Login → app lock setup → home with correct tabs for Employee vs Manager test users.
- Kill/reopen → app lock; background > 5 min → app lock.
- Deep link `hrms://inbox/<id>` from a test push opens the inbox route after unlock.
- Sync engine unit tests: retry/backoff, idempotent replays, state transitions.
- `flutter analyze` clean; unit + widget + golden tests pass; Patrol test for login + permission dialogs on Android emulator.

## Verify
```bash
cd src/Mobile/hrms_mobile
dart run build_runner build --delete-conflicting-outputs
flutter analyze && flutter test
flutter run --flavor dev -t lib/main_dev.dart
```

## Ask me first
- Final app name, icon, and bundle ids; Firebase projects for each flavor.
