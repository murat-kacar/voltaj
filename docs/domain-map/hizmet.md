# Domain Map: Hizmet

**Bounded Context**: Hizmet  
**Strategic Classification**: Core Subdomain (müşteriye verilen tüm saha/ofis hizmetleri Voltflow'un birincil iş sürecidir)  
**Depends on**: Identity (ActorId), Customers (CustomerId, SiteId), Finance (FaturaIptali — out-of-scope)  
**Depended on by**: Finance (fatura oluşturma), Inventory (malzeme tüketimi)

> **Deprecation Notu:** Bu bounded context, eski `quotes`, `work-orders` ve `projects` context'lerinin
> yerini almaktadır. `Teklif`, `İş Emri` ve `Proje` kavramları uçtan uca tek bir `Hizmet` nesnesi
> altında birleştirilmiştir. Teklif aşaması, Hizmet'in ilk evreleri (Draft/Issued) olarak ele alınır.

---

## Commands

| Command | Actor | Trigger Source |
|:---|:---|:---|
| `CreateDraft` | Manager / Admin | UI |
| `UpdateDraft` | Manager / Admin | UI |
| `Issue` | Manager / Admin | UI (generate and send PDF proposal) |
| `Accept` | Manager / Admin | UI (records customer verbal acceptance) |
| `Reject` | Manager / Admin | UI (records customer verbal rejection) |
| `Cancel` | Manager / Admin | UI (manual cancellation at any point) |
| `Revise` | Manager / Admin | UI (opens new record referencing rejected one) |
| `PayDeposit` | Manager / Admin | UI (records deposit payment) |
| `AddItem` | Manager / Admin / FieldTeam | UI |
| `RemoveItem` | Manager / Admin / FieldTeam | UI |
| `UpdateItem` | Manager / Admin / FieldTeam | UI |
| `Assign` | Manager / Admin | UI |
| `CheckIn` | FieldTeam | UI (mobile) |
| `CheckOut` | FieldTeam | UI (mobile) |
| `Hold` | Manager / Admin / FieldTeam | UI |
| `Resume` | Manager / Admin / FieldTeam | UI |
| `IssuePartialInvoice` | Manager / Admin / FieldTeam | UI |
| `Complete` | Manager / Admin / FieldTeam | UI |

---

## Domain Events

| Event | Produced by | Notes |
|:---|:---|:---|
| `ServiceDraftCreated` | `CreateDraft` | |
| `ServiceIssued` | `Issue` | PDF generated and sent to customer |
| `ServiceAccepted` | `Accept` | Triggers: service moves to Active Pool |
| `ServiceRejected` | `Reject` | This record becomes immutable |
| `ServiceCancelled` | `Cancel` | Manual cancellation; record closed |
| `ServiceRevised` | `Revise` | New record opened; `revision_of` references rejected id |
| `DepositPaid` | `PayDeposit` | Deposit amount recorded; Finance notified |
| `ItemAdded` | `AddItem` | Timestamp + note written to audit log (if Active) |
| `ItemRemoved` | `RemoveItem` | Timestamp + note written to audit log (if Active) |
| `ItemUpdated` | `UpdateItem` | Timestamp + note written to audit log (if Active) |
| `ServiceAssigned` | `Assign` | Assigned technician/team recorded |
| `CheckedIn` | `CheckIn` | Time tracking starts |
| `CheckedOut` | `CheckOut` | Time tracking ends |
| `ServiceOnHold` | `Hold` | Service paused |
| `ServiceResumed` | `Resume` | Service unpaused |
| `PartialInvoiceIssued` | `IssuePartialInvoice` | Amount, percentage, remaining updated; service stays Active |
| `ServiceCompleted` | `Complete` | Remaining balance invoiced; service closed |
| `OverpaymentDetected` | `RemoveItem` | Manual price + item removal causes negative remaining → Finance alert |

---

## Actors

| Actor | Permissions |
|:---|:---|
| `Admin` | All commands |
| `Manager` | All commands except full configuration |
| `FieldTeam` | AddItem, RemoveItem, UpdateItem, CheckIn, CheckOut, Hold, Resume, IssuePartialInvoice, Complete |
| `System` | Remaining limit calculation, PDF generation, auto-invoice on completion |

> **Note (K-01/K-02):** No customer portal. Customer acceptance/rejection is verbally communicated
> and recorded by Manager/Admin. Trigger source = "Customer declaration".
> Deposit (`PayDeposit`) exists in the initial phases.

---

## Policies (D4 — event → command reactions)

| Trigger Event | Reaction Command | Notes |
|:---|:---|:---|
| `ServiceAccepted` | Add to Active Services Pool | System automatic; state transition |
| `PartialInvoiceIssued` | Recalculate remaining limit | `remaining = current_total − total_billed` |
| `ServiceCompleted` | Generate Final Invoice + PDF | Amount = remaining limit (manual override allowed) |
| `PartialInvoiceIssued` | Generate Partial Invoice + PDF | Amount = specified; PDF issued |
| `DepositPaid` | Notify Finance | Finance records the payment |
| `OverpaymentDetected` | Create Finance Alert Item | Manual review in Finance dashboard; no auto credit note (decision B) |

---

## State Machine

```
[Draft] (Teklif Taslağı)
    │ Issue
    ▼
[Issued] (Teklif İletildi)
    ├─── Reject ──► [Rejected]  (terminal)
    │                  │
    │                  └── Revise ──► new [Draft] (revision_of reference)
    │
    ├─── Cancel ──► [Cancelled]  (terminal)
    │
    └─── Accept ──► [Active] (Hizmet Başladı)
                        │
                        ├── Assign ──► [Active/Assigned]
                        │       │
                        │       ├── CheckIn ──► [Active/InProgress]
                        │       │       └── CheckOut ──► [Active/Assigned]
                        │       │
                        │       └── Hold ──► [OnHold]
                        │               └── Resume ──► [Active/Assigned]
                        │
                        ├── IssuePartialInvoice ──► [Active]
                        │   (repeatable; remaining limit enforced — BR-02, BR-06)
                        │
                        └── Complete ──► [Completed]  (terminal)
                            (remaining balance invoiced; closed)
```

---

## Fatura Limit Kuralları (Active Aşamasında)

| Kural | Açıklama |
|:---|:---|
| **BR-01** | `kalan_limit = mevcut_kalem_toplamı − toplam_kesilen_fatura_tutarı` |
| **BR-02** | Kısmi fatura tutarı `kalan_limit`'i geçemez (normal durum — sistem engeller) |
| **BR-03** | Manuel fiyat müdahalesi varsa ve kalem silinmesi `kalan_limit`'i negatife düşürüyorsa: sistem engelleme yapmaz, `FazlaÖdemeDurumuOluştu` eventi fırlatılır |
| **BR-04** | `Complete` her zaman `kalan_limit` tutarında fatura keser (manuel fiyat ile override edilebilir) |
| **BR-05** | `Completed` statüsünden sonra kalem eklenemez, çıkartılamaz, fatura kesilemez |
| **BR-06** | `IssuePartialInvoice` işlemi sonucunda `kalan_limit = 0` olacaksa işlem engellenir. Sistem iki seçenek sunar: (a) tutarı azalt, (b) `Complete` kullan → %100 kesilir, hizmet kapanır |

---

## Read Models

| Read Model | Kullanım Yeri |
|:---|:---|
| `HizmetListesi` | Teklif, Aktif İşler ve Arşiv sekmelerini besleyen ana liste |
| `HizmetDetayı` | Hizmet detay sayfası (kalemler, faturalar, audit log) |
| `HizmetFaturaÖzeti` | Fatura paneli: toplam / kesilen / kalan |
| `HizmetRevizeyonZinciri` | Bir hizmetin tüm revizyon geçmişi (`revision_of` zincirleme) |
| `HizmetAuditLogu` | Kalem değişikliklerinin immutable geçmişi (tarih + not) |

---

## Aggregates & Entities

| Tür | Ad | Temel Değişmezler |
|:---|:---|:---|
| Aggregate | `Hizmet (Service)` | Statü makinesi guard ile korunur. Tekliften operasyona tek bir nesnedir. |
| Entity | `HizmetKalemi (ServiceItem)` | Draft aşamasında serbest değiştirilir; Active aşamasında audit zorunludur. |

---

## Ubiquitous Language

| Terim | Anlam | Karıştırılmamalı |
|:---|:---|:---|
| `Hizmet` | Teklif aşamasından fatura zincirine sahip tüm iş | Eski `İş Emri`, `Proje` veya `Teklif` kavramlarıyla |
| `AktifHizmetler` | Kabul edilmiş (`Active`), devam eden işler havuzu | Teklifler (`Draft`/`Issued`) |
| `KalanLimit` | `mevcut_toplam − toplam_kesilen`; sonraki faturanın üst sınırı | Orijinal teklif tutarı |
| `Revizyon` | Reddedilen teklife karşılık açılan yeni taslak | Güncelleme (eski kayıt asla değişmez) |

---

## Hotspots

| # | Soru | Durum |
|:---|:---|:---|
| H-HZ-01 | `KalanLimit` negatife düştüğünde Finance'e nasıl iletilecek? | **Kapalı** → `FazlaÖdemeDurumuOluştu` eventi Finance dashboard'unda manuel inceleme kalemi açar (B) |
| H-HZ-02 | Müşterinin teklifi doğrudan kabul/reddetmesi vs satış ekibinin kaydetmesi | **Kapalı** → Portal yok; Manager/Admin kaydeder |
| H-HZ-03 | Birden fazla kısmi fatura sonrası `kalan_limit = 0` yapacaksa ne olur? | **Kapalı** → BR-06: işlem engellenir, kullanıcı `Complete` kullanmak zorunda |
| H-HZ-04 | K-01 Kararı: API Rotaları nasıl olacak? | **Kapalı** → `/quotes` tamamen kaldırılıp, her şey `/services` rotasına birleştirilecek |
