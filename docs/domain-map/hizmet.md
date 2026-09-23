# Domain Map: Hizmet

**Bounded Context**: Hizmet  
**Strategic Classification**: Core Subdomain (müşteriye verilen tüm saha/ofis hizmetleri Voltflow'un birincil iş sürecidir)  
**Depends on**: Identity (ActorId), Customers (CustomerId, SiteId), Finance (FaturaIptali — out-of-scope)  
**Depended on by**: Finance (fatura oluşturma), Inventory (malzeme tüketimi)

> **Deprecation Notu:** Bu bounded context, eski `work-orders` ve `projects` context'lerinin
> yerini almaktadır. `İş Emri` ve `Proje` kavramları `Hizmet` altında birleştirilmiştir.
> Eski domain map dosyaları (`workorders.md`, `projects.md`) referans olarak saklanmaktadır.

---

## Commands

| Command | Actor | Trigger Source |
|:---|:---|:---|
| `CreateServiceDraft` | Manager / Admin | UI |
| `IssueQuote` | Manager / Admin | UI (generate and send PDF quote) |
| `AcceptQuote` | Manager / Admin | UI (records customer verbal acceptance) |
| `RejectQuote` | Manager / Admin | UI (records customer verbal rejection) |
| `CancelQuote` | Manager / Admin | UI (manual cancellation — no expiry date) |
| `ReviseService` | Manager / Admin | UI (opens new record referencing rejected one) |
| `PayDeposit` | Manager / Admin | UI (records deposit payment) |
| `AddItem` | Manager / Admin / FieldTeam | UI |
| `RemoveItem` | Manager / Admin / FieldTeam | UI |
| `UpdateItem` | Manager / Admin / FieldTeam | UI |
| `AssignService` | Manager / Admin | UI |
| `CheckIn` | FieldTeam | UI (mobile) |
| `CheckOut` | FieldTeam | UI (mobile) |
| `HoldService` | Manager / Admin / FieldTeam | UI |
| `ResumeService` | Manager / Admin / FieldTeam | UI |
| `IssuePartialInvoice` | Manager / Admin / FieldTeam | UI |
| `CompleteService` | Manager / Admin / FieldTeam | UI |

---

## Domain Events

| Event | Produced by | Notes |
|:---|:---|:---|
| `ServiceDraftCreated` | `CreateServiceDraft` | |
| `QuoteIssued` | `IssueQuote` | PDF generated and sent to customer |
| `QuoteAccepted` | `AcceptQuote` | Triggers: service added to Active Pool |
| `QuoteRejected` | `RejectQuote` | This record becomes immutable |
| `QuoteCancelled` | `CancelQuote` | Manual cancellation; record closed |
| `ServiceRevised` | `ReviseService` | New record opened; `revision_of` references rejected id |
| `DepositPaid` | `PayDeposit` | Deposit amount recorded; Finance notified |
| `ItemAdded` | `AddItem` | Timestamp + note written to audit log |
| `ItemRemoved` | `RemoveItem` | Timestamp + note written to audit log |
| `ItemUpdated` | `UpdateItem` | Timestamp + note written to audit log |
| `ServiceAssigned` | `AssignService` | Assigned technician/team recorded |
| `CheckedIn` | `CheckIn` | Time tracking starts |
| `CheckedOut` | `CheckOut` | Time tracking ends |
| `ServiceOnHold` | `HoldService` | Service paused |
| `ServiceResumed` | `ResumeService` | Service unpaused |
| `PartialInvoiceIssued` | `IssuePartialInvoice` | Amount, percentage, remaining updated; service stays Active |
| `ServiceCompleted` | `CompleteService` | Remaining balance invoiced; service closed |
| `OverpaymentDetected` | `RemoveItem` | Manual price + item removal causes negative remaining → Finance alert |

---

## Actors

| Actor | Permissions |
|:---|:---|
| `Admin` | All commands |
| `Manager` | CreateDraft, IssueQuote, AcceptQuote, RejectQuote, CancelQuote, ReviseService, PayDeposit, AddItem, RemoveItem, UpdateItem, AssignService, HoldService, ResumeService, IssuePartialInvoice, CompleteService |
| `FieldTeam` | AddItem, RemoveItem, UpdateItem, CheckIn, CheckOut, HoldService, ResumeService, IssuePartialInvoice, CompleteService |
| `System` | Remaining limit calculation, PDF generation, auto-invoice on completion |

> **Note (K-01/K-02):** No customer portal. Customer acceptance/rejection is verbally communicated
> and recorded by Manager/Admin. Trigger source = "Customer declaration".
> Deposit (`PayDeposit`) exists in the quote phase.

---

## Policies (D4 — event → command reactions)

| Trigger Event | Reaction Command | Notes |
|:---|:---|:---|
| `QuoteAccepted` | Add to Active Services Pool | System automatic; no user intervention |
| `PartialInvoiceIssued` | Recalculate remaining limit | `remaining = current_total − total_billed` |
| `ServiceCompleted` | Generate Final Invoice + PDF | Amount = remaining limit (manual override allowed) |
| `PartialInvoiceIssued` | Generate Partial Invoice + PDF | Amount = specified; PDF issued |
| `DepositPaid` | Notify Finance | Finance records the payment |
| `OverpaymentDetected` | Create Finance Alert Item | Manual review in Finance dashboard; no auto credit note (decision B) |

---

## State Machine

```
[Draft]
    │ IssueQuote
    ▼
[QuoteIssued]
    ├─── RejectQuote ──► [Rejected]  (terminal — immutable)
    │                        │
    │                        └── ReviseService ──► new [Draft] (revision_of reference)
    │
    ├─── CancelQuote ──► [Cancelled]  (terminal)
    │
    └─── AcceptQuote ──► [Active]
                             │
                             ├── AssignService ──► [Active/Assigned]
                             │       │
                             │       ├── CheckIn ──► [Active/InProgress]
                             │       │       └── CheckOut ──► [Active/Assigned]
                             │       │
                             │       └── HoldService ──► [OnHold]
                             │                └── ResumeService ──► [Active/Assigned]
                             │
                             ├── IssuePartialInvoice ──► [Active]  (service stays open)
                             │   (repeatable; remaining limit enforced — BR-02, BR-06)
                             │
                             └── CompleteService ──► [Completed]  (terminal)
                                 (remaining balance invoiced; closed)
```

---

## Fatura Limit Kuralları

| Kural | Açıklama |
|:---|:---|
| **BR-01** | `kalan_limit = mevcut_kalem_toplamı − toplam_kesilen_fatura_tutarı` |
| **BR-02** | Kısmi fatura tutarı `kalan_limit`'i geçemez (normal durum — sistem engeller) |
| **BR-03** | Manuel fiyat müdahalesi varsa ve kalem silinmesi `kalan_limit`'i negatife düşürüyorsa: sistem engelleme yapmaz, `FazlaÖdemeDurumuOluştu` eventi fırlatılır |
| **BR-04** | `HizmetTamamla` her zaman `kalan_limit` tutarında fatura keser (manuel fiyat ile override edilebilir) |
| **BR-05** | `Tamamlandı` statüsünden sonra kalem eklenemez, çıkartılamaz, fatura kesilemez |
| **BR-06** | `KısmiFaturaKes` işlemi sonucunda `kalan_limit = 0` olacaksa işlem engellenir. Sistem iki seçenek sunar: (a) tutarı azalt, (b) `HizmetTamamla` kullan → %100 kesilir, hizmet kapanır |

---

## Read Models

| Read Model | Kullanım Yeri |
|:---|:---|
| `AktifHizmetlerListesi` | Aktif Hizmetler Havuzu ekranı |
| `HizmetDetayı` | Hizmet detay sayfası (kalemler, faturalar, audit log) |
| `HizmetFaturaÖzeti` | Fatura paneli: toplam / kesilen / kalan |
| `HizmetRevizeyonZinciri` | Bir hizmetin tüm revizyon geçmişi (`revision_of` zincirleme) |
| `HizmetAuditLogu` | Kalem değişikliklerinin immutable geçmişi (tarih + not) |

---

## Aggregates & Entities

| Tür | Ad | Temel Değişmezler |
|:---|:---|:---|
| Aggregate | `Hizmet` | Statü makinesi guard ile korunur; `Reddedildi` ve `Tamamlandı` terminal statülerdir |
| Entity | `HizmetKalemi` | Aktif aşamada eklenip çıkartılabilir; her değişim audit log'a yazılır |
| Value Object | `FaturaKalemi` | Kesilen faturanın anlık kopyası; sonradan değiştirilemez |
| Value Object | `KalanLimitÖzeti` | `mevcut_toplam`, `toplam_kesilen`, `kalan` — her fatura/kalem değişiminde yeniden hesaplanır |

---

## Ubiquitous Language

| Terim | Anlam | Karıştırılmamalı |
|:---|:---|:---|
| `Hizmet` | Müşteriye verilen tek bir iş birimi (teklif → fatura zincirine sahip) | Eski `İş Emri` veya `Proje` kavramlarıyla — ikisi de artık `Hizmet`'tir |
| `HizmetKalemi` | Hizmet içindeki tek bir iş/malzeme kalemi | `FaturaKalemi` (faturaya yazılan anlık kopya) |
| `AktifHizmetlerHavuzu` | Kabul edilmiş, devam eden tüm hizmetlerin listesi | Arşiv (tamamlanmış hizmetler) |
| `KalanLimit` | `mevcut_toplam − toplam_kesilen`; sonraki faturanın üst sınırı | Orijinal teklif tutarı (kalemler değişince bu da değişir) |
| `KısmiFatura` | Hizmet açık kalırken kesilen kısmi fatura | `TamFatura` (hizmeti kapatan son fatura) |
| `Revizyon` | Reddedilen hizmete karşılık açılan yeni hizmet kaydı | Güncelleme (eski kayıt asla değişmez) |
| `FazlaÖdeme` | `kalan_limit` negatife düşünce oluşan durum | İptal (ayrı finans süreci) |

> **Karıştırılmamalı — Context farkları:**  
> `Hizmet` (bu context) ↔ `Fatura` (Finance context): Hizmet domain'i faturaları *tetikler*; faturaların iptali, muhasebe kaydı ve ödeme takibi Finance context'ine aittir.

---

## Hotspots

| # | Soru | Durum |
|:---|:---|:---|
| H-HZ-01 | `KalanLimit` negatife düştüğünde Finance'e nasıl iletilecek? | **Kapalı** → `FazlaÖdemeDurumuOluştu` eventi Finance dashboard'unda manuel inceleme kalemi açar (B); otomatik kredi notu/düşüm yok |
| H-HZ-02 | Müşterinin teklifi doğrudan kabul/reddetmesi (müşteri portalı) vs. satış ekibinin kaydetmesi — ikisi aynı Command mı? | **Kapalı** → Müşteri portalı yok; Manager/Admin müşteri beyanını kaydeder; tek command türü, trigger source = "Müşteri beyanı" |
| H-HZ-03 | Birden fazla kısmi fatura sonrası `KısmiFaturaKes` `kalan_limit = 0` yapacaksa ne olur? | **Kapalı** → BR-06: işlem engellenir, kullanıcı tutarı azaltmak veya `HizmetTamamla` kullanmak zorunda |

---

## Dışında Tutulanlar (Out of Scope)

- Fatura iptali → Finance context
- Ödeme tahsilatı → Finance context
- Müşteri/site yönetimi → Customers context
- Stok hareketi → Inventory context (Hizmet kalemi silindiğinde inventory'ye sinyal gider — ayrı saga)

---

*D1 D2 D5 D6 — Event Storming sonucu; `İş Emri` + `Proje` context'lerinin yerine geçer*
