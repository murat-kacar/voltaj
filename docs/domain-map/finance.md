# Domain Map — Finance (Billing & Payments)

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §2.1, §2.8 and
`voltflow-workflows-and-failure-modes.md` §5 (VUT `05.*`), cross-checked against
`src/Voltflow.Domain/Finance/FinanceModels.cs`, `BillingService.cs`/`BillingEndpoints.cs`,
`PaymentService.cs`/`PaymentEndpoints.cs` — one `Domain` folder backs both API surfaces, treated as a
single bounded context here.

## Classification (D6)

**Core subdomain.** Getting billing, deposits, and payment allocation right (no double-booking, no
over-allocation) is as central to the business as the field-service work itself.

## Ubiquitous language (D2)

- **Invoice** (`SalesInvoice`): what the *Customer* owes. **Not to be confused with**
  `PurchaseInvoice` (documented but unimplemented Suppliers area — see Customers context Hotspot #1)
  which is what Voltflow owes a supplier — same word, opposite direction of money.
- **Allocation**: assigning a `CustomerPayment` (or a Quote's deposit) against a specific
  `SalesInvoice`'s balance. **Not to be confused with** `StockAdjustment`'s "reservation" concept
  (Inventory context) — different resource being partitioned.
- **Ledger** (`CustomerLedgerEntry`): the append-only running-balance record per Party — parallel in
  spirit to Inventory's `StockMovement`, but for money instead of stock.
- **Deposit**: a Quote-stage prepayment (`DepositPaidAmount`), reconciled into the eventual Invoice's
  `AppliedDepositAmount` — a Quotes-context concept that Finance consumes, not originates.

## Actors

- Accounting/Office user (generate invoice, record payment, allocate)
- Technician/Dispatcher (indirectly, via `WorkOrder.ApproveForBilling` triggering invoice eligibility)

## Commands (imperative verb)

- `GenerateSalesInvoice` (from a billable WorkOrder, applying any Quote deposit)
- `IssueCreditNote` (reversal — invoices are never deleted, `VF-05102`)
- `ReceivePayment` (amount must be positive — `VF-05202`)
- `AllocatePaymentToInvoice` (blocked: over payment balance `VF-05302`, over invoice balance `VF-05303`,
  duplicate allocation `VF-05304`)

## Domain Events (past tense)

- `SalesInvoiceGenerated`
- `CreditNoteIssued`
- `PaymentReceived`
- `PaymentAllocated`

## Policies (event → command reaction)

- **`WorkOrderApprovedForBilling` (WorkOrders context) → `GenerateSalesInvoice`** is currently a
  direct, explicit office action (`POST /api/workorders/{id}/invoice`), not an automatic policy
  reaction — consistent with how Quotes→WorkOrder conversion also stays manual (see Quotes context
  Hotspot #1). Flagging the same pattern here for consistency, not as a new issue.

## Read Models

- `GetInvoiceById` (with `RemainingAmount` computed)
- `ListInvoices` / `ListPayments` (currently unbounded — V8, Phase 4)
- `GetCustomerLedger` (also surfaced from the Customers context by `PartyId`)

## Depends on / Depended on by (D5)

- **Depends on:** WorkOrders (billable completions), Quotes (deposits), Customers (`PartyId`),
  Identity (actor).
- **Depended on by:** Projects (progress-billing net-payable calculations feed the same ledger
  concepts).

## Hotspots (D3)

1. **OPEN:** Over-allocation guards (`VF-05302`–`VF-05304`) are enforced in the domain methods
   (`payment.Allocate`, `invoice.Allocate`) per the workflows doc — confirm these are also protected
   by DB-level constraints/optimistic concurrency (V5/V6) against two simultaneous allocation requests
   racing each other, not just in-process checks. Good candidate for a Q10 concurrency test (Phase 7).
