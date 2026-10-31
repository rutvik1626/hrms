# 05 — Workflow & Notification service

## Goal
A configurable multi-level approval engine used by every module, plus the notification pipeline (in-app, email, push) and the real-time SignalR hub.

## Read first
`CLAUDE.md`; requirements → Stage 1 "Platform (cross-cutting)" (workflow engine bullets), Stage 3 "Key patterns" (workflow integration), Stage 4 `hrms_workflow` tables, Stage 5 "Real-time (SignalR)", Stage 8 "G. Notifications & deep links", Stage 6 "Authorization" (segregation of duties).

## Scope
**In — Workflow engine**
- Definitions per tenant per request type (`LeaveRequest`, `AttendanceRegularization`, `CompOff`, `ExpenseClaim`, `Advance`, `AssetRequest`, `ProfileChange`, `Requisition`, `Offer`, `Resignation`, …) with ordered steps.
- Approver resolvers: reporting manager, Nth-level manager, department head, role (optionally scoped to the requester's location/legal entity), specific user. Uses a local **employee hierarchy projection** fed by corehr events.
- Conditions per step: amount/days thresholds, department, location, grade (simple JSON rule model with a typed evaluator — no scripting).
- Actions: approve, reject, send back (to requester for edit), delegate; comments required on reject/send back.
- Rules: requester can never approve own request (skip/redirect to next level); approver on leave → delegate if configured; **SLA escalation** via Hangfire (remind at X h, escalate at Y h); auto-approve option per step after Z days (off by default).
- Integration: consume `WorkflowRequested` (requestType, requestId, requesterEmployeeId, attributes for conditions, summary for display) → create instance → tasks; publish `WorkflowStepCompleted`, `WorkflowCompleted` (Approved/Rejected/Withdrawn); consume `WorkflowWithdrawn`.
- APIs: definitions CRUD (with default templates seeded on `TenantCreated`), `/me/tasks` (cursor + offset), task detail, approve/reject/send back/bulk approve, request timeline (`/workflow-instances/by-request/{type}/{id}`), delegations CRUD.

**In — Notifications**
- Templates per tenant per event/channel (Scriban or similar safe templating), seeded defaults, Transloco-style keys for future languages.
- Channels: in-app (notifications table), email (SMTP → Mailpit in dev, SES in prod via `IEmailSender`), push (FCM HTTP v1 via `IPushSender`; device tokens from identity events/internal API).
- User preferences (mute categories, approvals/payroll min "in-app").
- Consumes domain events from all services that trigger notifications (start with workflow events; modules add mappings later).
- Push payload: ids + generic text, deep link per Stage 8 table; no PII.
- SignalR hub `/hubs/notifications`: groups `user:{id}`, `tenant:{id}`; events `notificationReceived`, `taskCountChanged`, `jobProgress`, `approvalStatusChanged`; Redis backplane configurable.
- `/me/notifications` (cursor), mark read / read all, unread count.

**Out** — workflow designer UI (06/18 via settings screens), module-specific request handling (each module prompt).

## Acceptance criteria
- Integration test: a fake module publishes `WorkflowRequested` for a 2-step definition (Manager → HR when days > 3) → tasks created for correct approvers → approvals complete → `WorkflowCompleted` published once.
- Self-approval prevented; delegation and escalation work (Hangfire job tested with fake clock).
- Notifications: email captured in Mailpit; push sent through a fake sender with correct deep link; SignalR client receives `taskCountChanged`.
- Tenant isolation + permission matrix suites green.

## Verify
```bash
dotnet test tests/Workflow.UnitTests tests/Workflow.IntegrationTests
```
Open Mailpit (http://localhost:8025) after running the sample flow.

## Ask me first
- Default workflow templates per request type (proposal: Leave = Manager; Leave > 3 days = Manager → HR; Expense > ₹10,000 = Manager → Finance; Offer = Hiring Manager → HR Head).
- FCM project credentials (use fake sender until provided).
