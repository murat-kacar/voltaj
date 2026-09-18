# Domain Map — Inventory

Bounded context per D1/D2/D5/D6. Derived from `voltflow-domain-models.md` §2.5 and
`voltflow-workflows-and-failure-modes.md` §4 (VUT `04.*`), cross-checked against
`src/Voltflow.Domain/Inventory/` (`Inventory.cs`, `Warehouse.cs`), `InventoryService.cs`,
`InventoryEndpoints.cs`.

## Classification (D6)

**Supporting subdomain.** Stock tracking enables Core operations (WorkOrders consuming material,
Quotes pricing it) but is not itself where Voltflow differentiates — standard warehouse-management
patterns apply.

## Ubiquitous language (D2)

- **Material**: the catalog item (code, name, unit, VAT rate). **Not to be confused with**
  `WorkOrderMaterial`/`QuoteMaterialItem` (WorkOrders/Quotes contexts) — those are *usage snapshots*
  (quantity, price at the time), not the catalog record itself.
- **Stock (`QuantityOnHand`)** vs. **Reserved** vs. **Available**: three distinct numbers.
  `Available = OnHand - Reserved`. A reservation (`Reserve`/`Release`) never touches `OnHand` — only a
  `StockMovement` (an actual physical adjustment) does.
- **Movement**: an audited, signed adjustment to `QuantityOnHand` with a reason — the append-only
  ledger of physical stock changes, parallel in spirit to Finance's `CustomerLedgerEntry`.

## Actors

- Warehouse/Office user (adjust stock, reserve/release)
- WorkOrders context (consumes material via field usage — a cross-context caller, not a human actor)

## Commands (imperative verb)

- `AdjustStock` (blocked below zero — `VF-04101`)
- `ReserveStock` (blocked beyond available — `VF-04202`)
- `ReleaseStockReservation`

## Domain Events (past tense)

- `StockAdjusted`
- `StockReserved`
- `StockReservationReleased`

## Policies (event → command reaction)

None identified — every movement here is a direct command from a caller (human or the WorkOrders
context consuming material), not an automatic reaction to an event within this context.

## Read Models

- `GetMaterialByCode`
- `ListMaterials` / `ListLowStock` (currently unbounded — V8, Phase 4)
- `GetStockMovementHistory`

## Depends on / Depended on by (D5)

- **Depends on:** Identity (actor).
- **Depended on by:** WorkOrders (field material consumption), Quotes (material line pricing).

## Hotspots (D3)

1. **OPEN:** `StockMovement` and `StockBalance` update "tek transaction içinde yapılmalı" per the
   documented policy (`voltflow-domain-models.md` §5) — confirm this is enforced at the repository/
   `DbContext` level (H7-style single-transaction write), not assumed by convention only.
