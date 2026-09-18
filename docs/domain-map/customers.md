# Domain Map — Customers

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §1.1, §2.1 and
`voltflow-workflows-and-failure-modes.md` §2.1–2.2 (VUT `02.1.*`, `02.2.*`), cross-checked against
`src/Voltflow.Domain/Customers/`, `CustomerService.cs`, `CustomerEndpoints.cs`.

## Classification (D6)

**Core subdomain.** The customer relationship (and its ledger/history) is central to how Voltflow
differentiates as a field-service platform, not a commodity CRM concern.

## Ubiquitous language (D2)

- **Party**: the abstract concept covering Customer, CandidateCustomer, WalkInCustomer, Supplier, and
  ProjectParty — a shared identity anchor across contexts. Every Customer *is* a Party, but not every
  Party is a Customer.
- **Customer**: an *active* business party (`Type = Active`), reachable for quoting/billing.
  **Not to be confused with** *CandidateCustomer* (a Lead, not yet converted) or *AppUser* (Identity
  context — a system operator, never billed).
- **Site** (`CustomerSite`): a physical location belonging to a Customer. **Not to be confused with**
  `Project.SiteAddress` (Projects context) — a free-text address snapshot on a project, not a
  first-class linked entity.
- **Asset** (`CustomerAsset`): equipment (transformer, generator, panel) linked to a Site — the thing
  WorkOrders are performed *on*, distinct from the WorkOrder itself.

## Actors

- Office/Sales user (creates and activates customers, manages sites/assets)
- Admin

## Commands (imperative verb)

- `CreateCustomer` (as Lead)
- `ActivateCustomer` (Lead → Active)
- `LinkCustomerSite`
- `LinkCustomerAsset`

## Domain Events (past tense)

- `CustomerCreated`
- `CustomerActivated`
- `CustomerSiteLinked`
- `CustomerAssetLinked`

## Policies (event → command reaction)

None identified within this context alone. `CustomerActivated` is a precondition Quotes/WorkOrders
check (Read Model lookup), not something that triggers a command automatically here.

## Read Models

- `GetCustomerById`
- `ListCustomers` (currently unbounded — see V8, Phase 4)
- `GetCustomerLedger` (`CustomerLedgerEntry` history — Finance-owned data, read here by `PartyId`)
- `GetCustomerSitesAndAssets`

## Depends on / Depended on by (D5)

- **Depends on:** Identity (actor identity for `CreatedBy`/audit).
- **Depended on by:** Quotes, WorkOrders, Finance, Projects — all reference a Customer's `PartyId`.

## Hotspots (D3)

1. **OPEN:** `voltflow-domain-models.md` also documents a `Supplier`/`PurchaseInvoice`/
   `SupplierLedgerEntry` area (§2.7) that would naturally sit alongside Customers under the shared
   `Party` concept. No `Domain/Suppliers`, service, or endpoint exists yet — nothing to map under D1
   until a first operation is actually implemented. Flagged here so it isn't silently forgotten when
   that work starts.
2. **OPEN:** Duplicate-email rejection (`VF-02102`) is enforced at the application layer
   (`CustomerService`); confirm a DB-level unique constraint also exists (V5) — not verified while
   drafting this map.
