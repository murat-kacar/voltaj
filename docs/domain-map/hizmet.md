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
| `HizmetTaslağıOluştur` | SatışEkibi / Admin | UI |
| `HizmetTeklifEt` | SatışEkibi | UI (PDF teklif üret ve gönder) |
| `HizmetKabulEt` | SatışEkibi (müşteri kabulünü kaydeder) | UI |
| `HizmetReddet` | SatışEkibi (müşteri reddini kaydeder) | UI |
| `HizmetRevizeEt` | SatışEkibi | UI (reddedilen hizmete yanıt olarak) |
| `HizmeteKalemEkle` | SatışEkibi / SahaEkibi | UI |
| `HizmettekKalemiBırak` | SatışEkibi / SahaEkibi | UI |
| `HizmettekKalemiGüncelle` | SatışEkibi / SahaEkibi | UI |
| `KısmiFaturaKes` | SahaEkibi / Admin | UI |
| `HizmetTamamla` | SahaEkibi / Admin | UI |

---

## Domain Events

| Event | Produced by | Notlar |
|:---|:---|:---|
| `HizmetTaslağıOluşturuldu` | `HizmetTaslağıOluştur` | |
| `HizmetTeklifEdildi` | `HizmetTeklifEt` | PDF üretildi, müşteriye iletildi |
| `HizmetKabulEdildi` | `HizmetKabulEt` | Aktif Hizmetler Havuzuna otomatik eklenir |
| `HizmetReddedildi` | `HizmetReddet` | Bu kayıt artık immutable (değiştirilemez) |
| `HizmetRevizeEdildi` | `HizmetRevizeEt` | Yeni kayıt açılır; `revision_of` alanı reddedilen kaydın id'sini taşır |
| `KalemEklendi` | `HizmeteKalemEkle` | Tarih ve not ile audit log'a yazılır |
| `KalemBırakıldı` | `HizmettekKalemiBırak` | Tarih ve not ile audit log'a yazılır |
| `KalemGüncellendi` | `HizmettekKalemiGüncelle` | Tarih ve not ile audit log'a yazılır |
| `KısmiFaturaKesildi` | `KısmiFaturaKes` | Tutar, yüzde, kalan limit güncellendi |
| `HizmetTamamlandı` | `HizmetTamamla` | Kalan tutar faturası kesildi; hizmet kapandı |
| `FazlaÖdemeDurumuOluştu` | `HizmettekKalemiBırak` | Manuel fiyat müdahalesi + negatif kalan durumunda; Finance'e sinyal |

---

## Actors

| Actor | İzinler |
|:---|:---|
| `Admin` | Tüm komutlar |
| `SatışEkibi` | Taslak oluştur, Teklif et, Kabul/Reddet, Revize et, Kalem yönetimi |
| `SahaEkibi` | Kalem yönetimi (aktif aşamada), Kısmi Fatura Kes, Hizmeti Tamamla |
| `Sistem` | Kalan limit hesabı, PDF üretimi, Fatura otomatik oluşturma |

---

## Policies (D4 — event → command reactions)

| Trigger Event | Reaction Command | Notlar |
|:---|:---|:---|
| `HizmetKabulEdildi` | `HizmetAktifHavuzaEkle` | Sistem otomatik; kullanıcı müdahalesi yok |
| `KısmiFaturaKesildi` | `KalanLimitiGüncelle` | `kalan = mevcut_toplam − toplam_kesilen` |
| `HizmetTamamlandı` | `FaturaOluştur (Tam)` | Kalan limit tutarında fatura; PDF üretilir |
| `KısmiFaturaKesildi` | `FaturaOluştur (Kısmi)` | Belirtilen tutar; PDF üretilir |
| `FazlaÖdemeDurumuOluştu` | `FinansBildirimGönder` | Finans ekibine uyarı; iptal süreci onların inisiyatifinde |

---

## State Machine

```
[Taslak]
    │ HizmetTeklifEt
    ▼
[TeklifAşamasında]
    ├─── HizmetReddet ──► [Reddedildi]  (terminal — immutable)
    │                          │
    │                          └── HizmetRevizeEt ──► yeni [Taslak] (revision_of referanslı)
    │
    └─── HizmetKabulEt ──► [Aktif]
                               │
                               ├── KısmiFaturaKes ──► [Aktif]  (hizmet kapanmaz)
                               │   (tekrarlanabilir, kalan limit aşılamaz)
                               │
                               └── HizmetTamamla ──► [Tamamlandı]  (terminal)
                                   (kalan tutar tam fatura; kapatılır)
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
| H-HZ-01 | `KalanLimit` negatife düştüğünde Finance context'ine gönderilecek event'in payload'ı ve Finance'in bunu nasıl işleyeceği henüz tanımlanmadı | **Açık** |
| H-HZ-02 | Müşterinin teklifi doğrudan kabul/reddetmesi (müşteri portalı) vs. satış ekibinin kaydetmesi — ikisi aynı Command mı, ayrı ayrı mı? | **Açık** |
| H-HZ-03 | Birden fazla kısmi fatura sonrası `HizmetTamamla` çağrıldığında `kalan_limit = 0` ise fatura kesilmeli mi? | **Açık** |

---

## Dışında Tutulanlar (Out of Scope)

- Fatura iptali → Finance context
- Ödeme tahsilatı → Finance context
- Müşteri/site yönetimi → Customers context
- Stok hareketi → Inventory context (Hizmet kalemi silindiğinde inventory'ye sinyal gider — ayrı saga)

---

*D1 D2 D5 D6 — Event Storming sonucu; `İş Emri` + `Proje` context'lerinin yerine geçer*
