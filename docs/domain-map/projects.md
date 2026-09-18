# Domain Map — Projects (Progress Billing)

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §2.4 and
`voltflow-workflows-and-failure-modes.md` §6 (VUT `06.*`), cross-checked against
`src/Voltflow.Domain/Projects/Project.cs`, `ProjectService.cs`, `ProjectEndpoints.cs`.

## Classification (D6)

**Core subdomain.** Multi-phase contract billing (hakediş/progress billing) is a distinct, high-value
capability for larger jobs, separate from the single-visit WorkOrder flow.

## Ubiquitous language (D2)

- **Project**: a longer-running contract with phases and a target end date. **Not to be confused with**
  a `WorkOrder` — a `ProjectWorkOrder` links the two, but a Project is the billing/contract envelope, a
  WorkOrder is a unit of field execution that can (optionally) belong to one.
- **Progress Billing** (`ProgressBilling`): a periodic hakediş request against a Project phase — its
  own approval lifecycle, distinct from Quote acceptance or WorkOrder billing approval (three
  different things all colloquially called "approval").
- **Net Payable**: `ApprovedAmount - DeductionAmount`, computed at approval time — never allowed to go
  negative (`VF-06202`: approved amount can't be lower than deduction).

## Actors

- Project manager / Office (define project, phases, submit progress billing)
- Control engineer (`billing.Approve(approvedAmount)`)

## Commands (imperative verb)

- `CreateProject`
- `DefineProjectPhase`
- `SubmitProgressBilling`
- `ApproveProgressBilling` (blocked: approved amount below deduction — `VF-06202`)

## Domain Events (past tense)

- `ProjectCreated`
- `ProjectPhaseDefined`
- `ProgressBillingSubmitted`
- `ProgressBillingApproved`

## Policies (event → command reaction)

None identified — approval is a direct control-engineer action, not an automated reaction.

## Read Models

- `GetProjectById` (with phases)
- `ListProjects` (currently unbounded — V8, Phase 4)
- `GetProgressBillingHistory`

## Depends on / Depended on by (D5)

- **Depends on:** Customers (`PartyId`), WorkOrders (via `ProjectWorkOrder` linkage), Identity (actor).
- **Depended on by:** Finance (net-payable amounts feed the same ledger/payment concepts as
  WorkOrder-driven invoicing).

## Hotspots (D3)

1. **OPEN:** No domain event or read model was found connecting an approved `ProgressBilling` to an
   actual payment/invoice record in Finance — confirm whether that link is intentional (progress
   billing approval is itself the payable record) or a gap where Finance should be notified via an
   event/policy once M9's outbox pipeline is available for cross-context notification.
