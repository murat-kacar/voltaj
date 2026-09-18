# Domain Map — Quotes

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §2.2 and
`voltflow-workflows-and-failure-modes.md` §2.3–2.6 (VUT `02.3.*`–`02.6.*`), cross-checked against
`src/Voltflow.Domain/Quotes/`, `QuoteService.cs`, `QuoteEndpoints.cs`.

## Classification (D6)

**Core subdomain.** Pricing and quote-to-order conversion is a primary value-chain step (per
`voltflow-domain-models.md` §6's own "first six aggregates" list).

## Ubiquitous language (D2)

- **Quote**: a priced proposal in one of `Draft`/`Issued`/`Accepted`/`Rejected`/`Expired` — a terminal
  state machine (FSM). **Not to be confused with** a `WorkOrder` (WorkOrders context) — a Quote is a
  commercial offer; a WorkOrder is the field-execution record created *from* an accepted Quote.
- **Issue**: publishing a Draft quote to the customer (`Draft → Issued`). **Not to be confused with**
  "issuing" a SalesInvoice (Finance context) — different aggregate, different state machine.
- **Item**: one of `QuoteMaterialItem` / `QuoteLaborItem` / `QuoteServiceItem` — a priced line;
  distinguishing item *type* matters because each snapshots different source data (material code/name
  vs. plain description).
- **Change Order**: a follow-on Quote linked to an existing WorkOrder (`MarkAsChangeOrder`) for
  field-discovered scope changes — a Quote *of* the WorkOrders context's making, not a new concept.

## Actors

- Office/Sales user (draft, issue, manage items)
- Customer (accept, reject, pay deposit — via whatever external channel; not itself an `AppUser`)
- Technician (opens a Change Order from the field)

## Commands (imperative verb)

- `CreateQuoteDraft`
- `AddQuoteItem` (material | labor | service)
- `IssueQuote` (blocked if zero items — `VF-02302`)
- `AcceptQuote` (sets required deposit %)
- `RejectQuote` (reason required)
- `PayQuoteDeposit`
- `ExpireQuote` (blocked once in a terminal state)
- `ConvertQuoteToWorkOrder` (idempotent — repeat calls return the existing WorkOrder, `VF-02502`)
- `OpenChangeOrder` (from a WorkOrder)

## Domain Events (past tense)

- `QuoteDrafted`
- `QuoteItemAdded`
- `QuoteIssued`
- `QuoteAccepted`
- `QuoteRejected`
- `QuoteExpired`
- `QuoteConvertedToWorkOrder`
- `ChangeOrderOpened`

## Policies (event → command reaction)

None automatic today — conversion to WorkOrder is a direct, explicit command
(`POST /api/quotes/{id}/work-order`), not a background reaction to `QuoteAccepted`. Worth a Hotspot
(below): is same-transaction, user-triggered conversion the intended design permanently, or should
`QuoteAccepted` (+ deposit paid) eventually *trigger* WorkOrder creation as a Policy (M12 sub-operation)
instead of waiting for an explicit follow-up call?

## Read Models

- `GetQuoteById` (with items)
- `ListQuotes` (currently unbounded — see V8, Phase 4)

## Depends on / Depended on by (D5)

- **Depends on:** Customers (`PartyId`), Identity (actor).
- **Depended on by:** WorkOrders (`ConvertQuoteToWorkOrder` creates one), Finance (deposit payments
  land in `CustomerLedgerEntry`/`SalesInvoice` reconciliation).

## Hotspots (D3)

1. **OPEN:** Conversion to WorkOrder is manually triggered, not policy-driven — see Policies section
   above. Not necessarily wrong, but worth an explicit decision before M12 work (Phase 3) formalizes
   sub-operations elsewhere, so this context is treated consistently with the others.
2. **OPEN:** `ExpireQuote`'s terminal-state guard ("terminal durumlardaki teklifler süresi dolsa dahi
   expire edilemez" per workflows doc) implies a background sweep for due-date expiry is expected
   somewhere (comparable to `MaintenanceProcessor` in WorkOrders) — no such worker was found in
   `src/Voltflow.Worker/`. Confirm whether quote expiry is currently manual-only.
