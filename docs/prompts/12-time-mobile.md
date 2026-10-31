# 12 — Mobile: attendance & leave

## Goal
Deliver the mobile punch flow (GPS + geofence + selfie, offline-first), attendance calendar, regularization and leave features.

## Read first
`CLAUDE.md`; requirements → Stage 8 "Feature matrix", "Screen inventory" items 2–4, "A. Check-in / check-out" (client side), "B. Offline sync engine", "C. Leave application", "D. Regularization", "Privacy & compliance".

## Scope
**In**
- Regenerate Dart client.
- **Home**: big Check-in/Check-out button with live timer, today's shift, quick actions, balance chips, pending-sync banner.
- **Punch flow**: permission checks → best GPS fix (≤ 50 m, 15 s timeout) → mock-location block → geofence check (client copy of geofences from bootstrap cache) → outside-geofence reason (if policy allows) → selfie screen (front camera only, ML Kit face count = 1 + blink liveness, retake) → save to `pending_punches` first → immediate success UI → sync (selfie upload then batch POST). Capture `elapsedSinceBoot` and last server-time offset for time integrity.
- Punch result + status updates (Valid/Flagged/Rejected) when sync response arrives; flagged reason shown.
- Attendance calendar (month, colour-coded), day detail (punches with map/selfie thumbnail), pending sync list, regularization form.
- Leave: balances, apply (type → dates → half-day → reason → attachment via camera/file; server preview), my leaves + timeline, cancel, holiday list (optional holiday pick), comp-off request.
- Cache: profile, balances (with "as of"), holidays, geofences, today's shift, last 2 months attendance — stale-while-revalidate.
- Notifications deep links for leave/regularization/punch flagged/missed punch.

## Acceptance criteria
- Airplane mode: punch saved, banner shows "1 pending"; back online → synced automatically; replay doesn't duplicate.
- Mock location → blocked; outside geofence → blocked or flagged per policy.
- Unit tests: geofence math (haversine with accuracy tolerance), time-integrity payload, queue state machine. Widget/golden tests for home + punch screens. Patrol: punch with mocked location + camera on emulator.

## Verify
```bash
cd src/Mobile/hrms_mobile && ./tool/generate_api.sh && dart run build_runner build -d
flutter analyze && flutter test && patrol test --flavor dev
```

## Ask me first
- Liveness strictness (blink only vs blink + head turn) and whether server face-match is offered in v1.
