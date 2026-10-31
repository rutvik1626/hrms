# 11 — Time web screens (Attendance + Leave)

## Goal
Deliver attendance and leave screens in Angular for employees, managers and HR.

## Read first
`CLAUDE.md`; requirements → Stage 7 "Screen inventory" (Time line), "Standard page patterns" (calendar, request + approval, settings page), "Dashboards" (Employee, Manager widgets).

## Scope
**In**
- Regenerate API client.
- Employee: web check-in widget (dashboard), **my attendance calendar** (colour legend, day detail with punches/map/selfie), regularization form, apply leave (live preview from `/preview`), my leaves with timeline, cancel, balances, holiday list + optional holiday selection, comp-off request.
- Manager: team attendance (today board + month grid), team leave calendar (FullCalendar), approvals render in Inbox (register detail components for leave/regularization/comp-off in the inbox registry), bulk approve.
- HR: attendance register (filters, export), exceptions (late, missing punch), **flagged punch review** (map + selfie + reasons), shift master, **roster planner** (grid: employees × dates, drag/bulk assign), weekly-off patterns, attendance policies, **geofence map editor** (Leaflet/OSM: draw circle, set radius), biometric devices + mapping, holiday calendars, leave types & policies (settings page with effective-from), balance adjustments, opening balance import, **month-end lock** screen (checklist: pending regularizations/leaves, flagged punches → lock).
- Dashboard widgets: who's in/out/on leave, pending approvals, my balances, upcoming holidays.

## Acceptance criteria
- Leave apply shows working-day count and balance after before submit; server validation errors map to fields.
- Manager approves from Inbox; employee's calendar updates live via SignalR.
- Month lock blocked while pending items exist (unless overridden with permission + reason).
- Playwright: employee applies 3-day leave → manager approves → HR approval (days > 3 rule) → balance reduced → calendar shows leave.

## Verify
```bash
cd src/Web/hrms-web && npm run api:generate && npm run lint && npm test && npx playwright test time
```

## Ask me first
- Calendar colour legend preferences (otherwise: Present green, Absent red, Leave blue, Holiday purple, WO grey, Half-day amber, Flagged orange).
