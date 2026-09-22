# Domain Map: Customers

**Bounded Context**: Customers  
**Strategic Classification**: Core Subdomain (müşteri ilişkisi Voltflow'un temel değeri)  
**Depends on**: Identity (actor)  
**Depended on by**: Quotes, WorkOrders, Finance, Sales  

---

## Commands

| Command | Actor | Trigger Source |
|:---|:---|:---|
| `CreateCustomer` | Manager / Admin | UI (CustomersView) |
| `UpdateCustomer` | Manager / Admin | UI |
| `ConvertLeadToActive` | Manager / Admin | UI |
| `DeactivateCustomer` | Admin | UI |
| `CreateCustomerSite` | Manager / Admin | UI (CustomerDetailView) |
| `UpdateCustomerSite` | Manager / Admin | UI |
| `CreateCustomerAsset` | Manager / Admin | UI |
| `UpdateCustomerAsset` | Manager / Admin | UI |
| `MarkAssetResolved` | Technician / Manager | UI (WorkOrder completion) |

## Domain Events

| Event | Produced by |
|:---|:---|
| `CustomerCreated` | `CreateCustomer` |
| `CustomerUpdated` | `UpdateCustomer` |
| `LeadConverted` | `ConvertLeadToActive` |
| `CustomerDeactivated` | `DeactivateCustomer` |
| `SiteCreated` | `CreateCustomerSite` |
| `AssetCreated` | `CreateCustomerAsset` |
| `AssetMarkedResolved` | `MarkAssetResolved` |

## Actors

| Actor | Permission |
|:---|:---|
| `Admin` | Tüm işlemler |
| `Manager` | Create/Update/Convert |
| `Technician` | Read, MarkAssetResolved |

## Policies

| Trigger Event | Reaction | Notes |
|:---|:---|:---|
| `LeadConverted` | — | Quotes ve WorkOrders için müşteri artık seçilebilir |

## Read Models

| Read Model | Used by |
|:---|:---|
| `CustomerDto` | Listing, dropdown seçimler |
| `CustomerDetailView` | Detail page |
| `CustomerAssetDto` | WorkOrder, Quote bağlantısı |

## Aggregates & Entities

| Type | Name | Key Invariants |
|:---|:---|:---|
| Aggregate | `Customer` | `FullName` + `Phone` zorunlu; `Email` optional; `TaxNumber` unique (DB) |
| Entity | `CustomerSite` | Bir müşteriye ait adres/konum |
| Entity | `CustomerAsset` | Sahada servis edilen ekipman |
| Value Object | `CustomerType` (enum) | `Lead` → `Active` tek yönlü geçiş |

## Ubiquitous Language

| Term | Meaning | Not to be confused with |
|:---|:---|:---|
| `Customer` | Voltflow'a kayıtlı dış müşteri | `AppUser` — iç sistem kullanıcısı |
| `Lead` | Henüz aktif sözleşmesi olmayan aday müşteri | CRM'deki genel "lead" kavramı |
| `Asset` | Müşterinin servis edilecek ekipmanı | Muhasebe varlığı |
| `Site` | Müşterinin hizmet alacağı lokasyon | Şirket merkezi |

## Hotspots

| # | Soru | Durum |
|:---|:---|:---|
| H-C-01 | Müşteri silme (soft/hard delete)? Şu an `SetInactive()` var, hard delete yok. | Açık |
| H-C-02 | TaxNumber unique kısıtı: aynı şirketin birden fazla müşteri kaydı senaryosu? | Açık |

---

*D1 D2 D5 D6 · Graphify community `Customer` (cohesion 0.10, 19 node)*
