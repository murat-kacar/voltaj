# Domain Map — Reminders

Bounded context per D1/D2/D5/D6. Derived from `voltflow-workflows-and-failure-modes.md` §7.3 (VUT
`07.3.*`), cross-checked against `src/Voltflow.Domain/Reminders/ReminderModels.cs`,
`Voltflow.Worker/ReminderProcessor.cs` + `ReminderWorker.cs`. No dedicated `Service`/`Endpoints` pair
was found — this context is currently driven entirely by the background worker, not a direct user-facing
API; confirm that's intentional (see Hotspot).

## Classification (D6)

**Supporting subdomain.** SLA/reminder tracking makes other contexts more reliable (nudging overdue
work) but isn't itself a differentiator — a classic cross-cutting supporting capability.

## Ubiquitous language (D2)

- **Reminder**: a scheduled nudge tied to another context's overdue state (e.g. an overdue WorkOrder).
  **Not to be confused with** a Domain Event (e.g. `WorkOrderAssigned`) — a Reminder is a
  *time-triggered* record this context owns, not a notification-of-something-that-already-happened
  owned by the source context.

## Actors

- System (`ReminderProcessor`, scheduled)
- Office/Dispatcher (implicit recipient — delivery channel not confirmed, see Hotspot)

## Commands (imperative verb)

- `ScheduleReminder` (created by policy, not directly user-invoked today)
- `TriggerReminder` (processor marks it fired, reschedules next occurrence)

## Domain Events (past tense)

- `ReminderScheduled`
- `ReminderTriggered`

## Policies (event → command reaction)

- **`<source-context-overdue-condition>` → `ScheduleReminder`/`TriggerReminder`**: per the workflows
  doc, "geciken işler taranır" (overdue jobs are scanned) — this is a Policy in D4's sense, but its
  exact trigger condition (which contexts' overdue states it watches — WorkOrders only? Quotes
  expiry? Progress Billing due dates?) wasn't confirmed from the code reviewed so far.

## Read Models

- `ListDueReminders` (worker-internal; no user-facing read model confirmed)

## Depends on / Depended on by (D5)

- **Depends on:** WorkOrders (and possibly Quotes/Projects — see Hotspot) as the upstream source of
  "overdue" state it watches.
- **Depended on by:** none identified (terminal — output is a notification, not consumed by another
  context's domain logic).

## Hotspots (D3)

1. **OPEN:** No `Service`/`Endpoints` pair exists for Reminders — it's driven purely by
   `ReminderWorker`/`ReminderProcessor`. Confirm there's genuinely no user-facing management surface
   planned (view/dismiss/snooze a reminder), or whether this is simply not built yet.
2. **OPEN:** The exact set of upstream contexts/conditions this policy watches wasn't confirmed from
   code — needs a short pass over `ReminderProcessor.cs` to pin down precisely, which affects the
   Depends-on list above.
