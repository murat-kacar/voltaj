# Domain Map: Work Orders

**Bounded Context**: Work Orders  
**Strategic Classification**: Core Subdomain (saha operasyonu Voltflow'un ana iş sürecidir)  
**Depends on**: Identity (actor, AssignedUserId), Customers (CustomerId, SiteId, AssetId), Quotes (SourceQuoteId), Projects (ProjectId, PhaseId)  
**Depended on by**: Finance (invoicing), Reminders (maintenance triggers)  

---

## Commands

| Command | Actor | Trigger Source |
|:---|:---|:---|
| `CreateWorkOrder` | Manager / Admin | UI (CreateWorkOrderModal) / Policy (QuoteAccepted) |
| `AssignWorkOrder` | Manager / Admin | UI |
| `MarkEnRoute` | Technician | UI (mobile) |
| `ReportNoShow` | Technician / Manager | UI |
| `CompleteSafetyChecklist` | Technician | UI |
| `StartWorkOrder` | Technician | UI |
| `CheckIn` | Technician | UI |
| `CheckOut` | Technician | UI |
| `PutOnHold` | Technician / Manager | UI |
| `ResumeWorkOrder` | Technician / Manager | UI |
| `CancelWorkOrder` | Manager / Admin | UI |
| `CompleteWorkOrder` | Technician | UI (signature / photo required) |
| `ApproveForBilling` | Manager / Admin | UI |
| `InvoiceWorkOrder` | Admin / Billing | System / UI |
| `AddWorkOrderItem` | Technician / Manager | UI |
| `LinkSourceQuote` | System | Policy (QuoteAccepted) |
| `LinkToProject` | Manager | UI |
| `LinkToSiteAndAsset` | Manager / Technician | UI |
| `LinkToParentWorkOrder` | Manager | UI (sub-work order) |

## Domain Events

| Event | Produced by |
|:---|:---|
| `WorkOrderCreated` | `CreateWorkOrder` |
| `WorkOrderAssigned` | `AssignWorkOrder` |
| `WorkOrderStarted` | `StartWorkOrder` |
| `WorkOrderOnHold` | `PutOnHold` |
| `WorkOrderResumed` | `ResumeWorkOrder` |
| `WorkOrderCompleted` | `CompleteWorkOrder` |
| `WorkOrderApprovedForBilling` | `ApproveForBilling` |
| `WorkOrderInvoiced` | `InvoiceWorkOrder` |
| `WorkOrderCancelled` | `CancelWorkOrder` |
| `WorkOrderNoShow` | `ReportNoShow` |

## Actors

| Actor | Permission |
|:---|:---|
| `Admin` | Tüm işlemler |
| `Manager` | Create, Assign, Approve, Link |
| `Technician` | EnRoute, Safety, Start, CheckIn/Out, Hold, Complete |
| `Scheduler` | Maintenance contract triggers |

## Policies (D4 — sub-operations)

| Trigger Event | Reaction Command | Notes |
|:---|:---|:---|
| `QuoteAccepted` | `CreateWorkOrder` | M12 — parent QuoteId propagated |
| `MaintenanceContractDue` | `CreateWorkOrder` | M12 — scheduler sub-op |

## State Machine

```
Open → Assigned → EnRoute → InProgress → Completed → ReadyForBilling → Invoiced
                               ↕ OnHold
Open/Assigned/EnRoute/InProgress/OnHold → Cancelled
EnRoute/Assigned → NoShow
```

## Read Models

| Read Model | Used by |
|:---|:---|
| `WorkOrderSummary` | OperationsViews list |
| `WorkOrderDetailDto` | WorkOrderDetailPage |
| `WorkOrderTimeEntry` | Time tracking panel |

## Aggregates & Entities

| Type | Name | Key Invariants |
|:---|:---|:---|
| Aggregate | `WorkOrder` | Status machine enforced via `SetStatus()` guard; Invoiced is terminal |
| Entity | `WorkOrderItem` | description + quantity + unitPrice; added only to non-terminal orders |
| Entity | `WorkOrderTimeEntry` | Check-in/out pairs; auto-close on hold/cancel/complete |
| Aggregate | `MaintenanceContract` | Periyodik iş emirleri üretir (Scheduler policy) |

## Ubiquitous Language

| Term | Meaning | Not to be confused with |
|:---|:---|:---|
| `WorkOrder` | Sahadaki bir hizmet görevi | Project'teki Phase (süreç adımı) |
| `CheckIn` | Teknisyenin işe başladığını kaydetmesi (zaman) | Müşteri check-in |
| `Hold` | Geçici duraklatma (malzeme bekleme vb.) | Cancel (kalıcı) |
| `ProofOfWork` | Tamamlamayı kanıtlayan imza/fotoğraf | — |
| `NoShow` | Müşteri yoktu — göreve erişilemedi | Cancellation (kasıtlı iptal) |
| `ReadyForBilling` | Tamamlandı ama henüz faturalanmadı | Invoiced (faturası kesildi) |

## Hotspots

| # | Soru | Durum |
|:---|:---|:---|
| H-WO-01 | Sub-work order (parentId) — M12 kuralına göre her biri bağımsız operasyon olmalı. Şu an sadece link var, Command+audit yok. | **Bloke (D3)** |
| H-WO-02 | H10 compensation: `WorkOrderCancelled` sonrası InvoiceWorkOrder partial tamamlandıysa ne olur? | Açık |
| H-WO-03 | `MaintenanceContract` kendi bounded context'i mi yoksa WorkOrders'ın parçası mı? | Açık |

---

*D1 D2 D5 D6 · Graphify community `WorkOrder` (cohesion 0.06, 36 node)*
