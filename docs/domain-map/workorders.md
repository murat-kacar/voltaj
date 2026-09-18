# Domain Map — WorkOrders

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §2.3, §2.6 and
`voltflow-workflows-and-failure-modes.md` §3 + §7.1 (VUT `03.*`, `07.1.*`), cross-checked against
`src/Voltflow.Domain/WorkOrders/` (`WorkOrder.cs`, `WorkOrderTimeEntry.cs`, `MaintenanceContract.cs`),
`WorkOrderService.cs`, `WorkOrderEndpoints.cs`, `Voltflow.Worker/MaintenanceProcessor.cs`.

## Classification (D6)

**Core subdomain.** The field-service execution lifecycle (dispatch → safety gate → time tracking →
completion → billing handoff) is Voltflow's primary differentiator — this is the richest FSM in the
system.

## Ubiquitous language (D2)

- **WorkOrder**: the field-execution record, `Open → Assigned → EnRoute/InProgress → ... → Invoiced`
  (terminal) or `Cancelled`/`NoShow`. **Not to be confused with** a `Quote` (a commercial offer that
  precedes it) or a `Project` (Projects context — a longer-running contract a WorkOrder can belong to
  via `ProjectWorkOrder`, but is not itself).
- **Complete** vs. **Invoiced**: `Complete` means field work is done (signature/photo captured);
  `Invoiced` is a separate, later, Finance-driven terminal state reached only after office
  `ApproveForBilling`. A `Completed` order is *not yet* billed.
- **Hold**: a temporary pause (`OnHold`) with automatic time-entry compensation (open check-ins are
  auto-closed) — distinct from `Cancel`, which is terminal-adjacent and only legal from non-terminal
  states.
- **Maintenance Contract**: a recurring-schedule definition that *produces* WorkOrders via a policy —
  not a WorkOrder itself.

## Actors

- Dispatcher/Office (assign, approve-for-billing)
- Technician (en-route, start, check-in/out, hold/resume, add material, complete)
- Operations manager (approve-for-billing)
- System (MaintenanceProcessor — scheduled policy trigger)

## Commands (imperative verb)

- `AssignWorkOrder`
- `MarkEnRoute` / `ReportNoShow` (reason required)
- `CompleteSafetyChecklist`
- `StartWorkOrder` (blocked without safety checklist — `VF-03201`)
- `CheckIn` / `CheckOut` (blocked: no double check-in — `VF-03302`)
- `PutOnHold` (reason required) / `ResumeWorkOrder`
- `AddFieldMaterial`
- `CompleteWorkOrder` (signature or photo required — `VF-03401`)
- `ApproveForBilling`
- `InvoiceWorkOrder` (blocked unless `ReadyForBilling` — `VF-03501`)
- `CancelWorkOrder` (reason required; blocked once `Completed`/`Invoiced` — `VF-03504`)
- `LinkToParentWorkOrder` (warranty/callback)
- `CreateMaintenanceContract`

## Domain Events (past tense)

- `WorkOrderAssigned`, `WorkOrderEnRoute`, `WorkOrderNoShowReported`
- `SafetyChecklistCompleted`, `WorkOrderStarted`
- `WorkOrderCheckedIn`, `WorkOrderCheckedOut`
- `WorkOrderPutOnHold`, `WorkOrderResumed`
- `FieldMaterialAdded`
- `WorkOrderCompleted` (→ recorded to Outbox as `EVT_03402`)
- `WorkOrderApprovedForBilling`
- `WorkOrderInvoiced`
- `WorkOrderCancelled`
- `MaintenanceContractDue`

## Policies (event → command reaction)

- **`MaintenanceContractDue` → `CreateWorkOrder`** (M12 sub-operation): `MaintenanceProcessor` scans
  active contracts on schedule; when due, creates a new WorkOrder and pushes `NextMaintenanceDate`
  forward by the contract's recurrence interval. This is D4's textbook example of a Policy.
- **`WorkOrderPutOnHold` → auto-close open time entry**: an in-progress check-in is automatically
  checked out (compensating side-effect within the same command, not a separate async policy today —
  worth confirming that's intentional vs. should be its own tracked compensation per H10/H11, Phase 3).

## Read Models

- `GetWorkOrderById`
- `ListWorkOrders` (role-scoped: technicians see only assigned orders — currently unbounded, V8 Phase 4)
- `GetWorkOrderTimeEntries`

## Depends on / Depended on by (D5)

- **Depends on:** Quotes (created from an accepted Quote), Customers (`PartyId`), Inventory (field
  material consumption), Identity (actor).
- **Depended on by:** Finance (`ApproveForBilling`/`Invoice` feed `SalesInvoice`), Reminders (SLA/overdue
  tracking watches WorkOrder state).

## Hotspots (D3)

1. **OPEN:** The Hold→auto-checkout compensation happens inline in the domain method, not as an
   independently auditable compensating operation. Revisit once H10/H11 (Phase 3) formalize what a
   "first-class compensating operation" means here — this may already satisfy the intent, or may need
   its own Command/audit trail.
2. **OPEN:** `LinkToParentWorkOrder` (warranty/callback) creates a new WorkOrder referencing a closed
   one — confirm this is modeled as a genuinely new `command_id`/`operation_id` (M11) rather than a
   mutation of the closed record (which the FSM's terminal-state protection should otherwise forbid).
