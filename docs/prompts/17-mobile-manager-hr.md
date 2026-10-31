# 17 — Mobile: Manager, HR-lite & remaining self-service

## Goal
Complete the mobile app: approvals inbox, team views, HR-lite tools, and the remaining employee screens (profile, documents, directory, resignation, interview feedback).

## Read first
`CLAUDE.md`; requirements → Stage 8 "Feature matrix" (Manager + HR columns), "Navigation", "Screen inventory" items 8–12, "F. Approvals", "G. Notifications & deep links".

## Scope
**In**
- **Inbox**: grouped list (leave, regularization, comp-off, expense, asset, profile change, resignation), badge via SignalR/push, detail with decision context (balance, team overlap, policy violations, receipts), approve/reject/send back with comment, **bulk approve** (leave/regularization), optimistic removal + 5 s undo.
- **Team**: today board (in/out/leave/absent), member list → scoped profile → attendance & leave, team leave calendar.
- **HR-lite**: employee search (scoped), location attendance summary, flagged punch review (map + selfie, approve/reject), HR KPI dashboard.
- **Employee**: profile tabs + change requests (bank change with step-up MFA via hosted flow), documents (view/upload), directory (tap-to-call/email), resignation submission + exit checklist status, assigned interviews + feedback form (Interviewer), announcements, birthdays/anniversaries.
- Deep links for every notification type in Stage 8 table.

## Acceptance criteria
- Manager approves a leave on mobile → employee gets push → web calendar updates.
- HR (Location scope) sees only their location in search and summaries.
- Patrol: inbox approve + undo; widget/golden tests for inbox, team, profile screens; `flutter analyze` clean.

## Verify
```bash
cd src/Mobile/hrms_mobile && flutter analyze && flutter test && patrol test --flavor dev
```

## Ask me first
- Whether managers may approve expense claims above a threshold on mobile (default: yes, with receipt view).
