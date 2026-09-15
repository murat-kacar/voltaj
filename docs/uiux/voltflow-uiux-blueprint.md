# Voltflow UI/UX Blueprint

## 1. Amaç

Voltflow, küçük ve orta ölçekli işletmelerin müşteri, teklif, iş emri, stok, proje, ödeme ve operasyon süreçlerini takip ettiği çalışan odaklı bir uygulamadır.

Arayüz hedefi:

- Günlük operasyonu hızlandırmak
- İş durumunu tek bakışta anlaşılır kılmak
- Kullanıcıyı doğru sıradaki aksiyona yönlendirmek
- Rol bazlı yetkiyi görünür ve güvenli uygulamak
- Hataları, bekleyen onayları ve geciken işleri saklamamak
- Audit ve operation lineage bilgisini operasyonel bir araç olarak sunmak

Bu belge frontend implementation başlamadan önceki UI/UX sözleşmesidir.

## 2. Tasarım ilkeleri

### 2.1 Operasyon öncelikli tasarım

İlk ekran pazarlama veya tanıtım sayfası değildir. Kullanıcı doğrudan iş listesine, bekleyen aksiyonlara ve durum özetine ulaşır.

### 2.2 Yoğun ama okunabilir bilgi

Tekrarlı kullanım için:

- Kompakt tablolar
- Filtreler
- Durum etiketleri
- Satır aksiyonları
- Klavye erişimi
- Hızlı oluşturma akışları

kullanılır.

### 2.3 Durum her zaman görünür

Her kaynak için:

- Loading
- Empty
- Success
- Validation error
- Unauthorized
- Forbidden
- Conflict
- Retryable failure

durumları tasarlanır.

### 2.4 Rol güvenliği görünür olmalı

Kullanıcı erişemediği aksiyonu görmemeli veya neden göremediğini güvenli bir mesajla anlamalıdır. Frontend gizleme yalnızca UX katmanıdır; gerçek yetki API tarafındadır.

### 2.5 Kaynak ve işlem geçmişi erişilebilir olmalı

Her kritik detay ekranında:

- Kim oluşturdu?
- Ne zaman oluşturdu?
- Kim güncelledi?
- Hangi operation tarafından üretildi?
- Hangi ekran ve aksiyon tetikledi?
- Önceki/sonraki değerler nelerdi?

bilgilerine erişim yolu bulunmalıdır.

## 3. Bilgi mimarisi

Ana navigasyon:

1. Dashboard
2. Customers
3. Quotes
4. Work Orders
5. Inventory
6. Projects
7. Payments
8. Audit / Operations
9. Admin

### 3.1 Rol bazlı navigasyon

| Alan | Viewer | Technician | Manager | Admin |
|---|---:|---:|---:|---:|
| Dashboard | Görür | Görür | Görür | Görür |
| Customers | Salt-okuma | Salt-okuma | Yönetir | Yönetir |
| Quotes | Salt-okuma | Salt-okuma | Yönetir | Yönetir |
| Work Orders | Salt-okuma | Atandığı işleri | Yönetir | Yönetir |
| Inventory | Salt-okuma | Operasyonel kullanım | Yönetir | Yönetir |
| Projects | Salt-okuma | İlgili operasyon | Yönetir | Yönetir |
| Payments | Salt-okuma | Gizli | Yönetir | Yönetir |
| Audit | Kısıtlı | Kendi operasyonu | Operasyon kapsamı | Tam erişim |
| Admin | Yok | Yok | Yok | Tam erişim |

## 4. Ekran matrisi

### 4.1 Authentication

- Login
- Pending approval
- Session revoked
- Password reset request
- Password reset completion
- Unauthorized
- Forbidden

### 4.2 Dashboard

İlk görünüm:

- Bugünkü iş emirleri
- Atanmış açık işler
- Bekleyen customer approval/conversion
- Draft/issued quote özeti
- Kritik stok uyarıları
- Vadesi yaklaşan payment/reminder
- Dead-letter/outbox uyarısı, yalnız yetkili kullanıcılar için

### 4.3 Customers

Ekranlar:

- Customer list
- Candidate list
- Customer detail
- Candidate detail
- Conversion confirmation
- Customer notes/history

Ana akış:

```text
Candidate oluştur
  -> Candidate detayını aç
  -> Bilgileri kontrol et
  -> Convert Customer
  -> Başarı sonucu ve yeni Customer detayına git
```

### 4.4 Quotes

Ekranlar:

- Quote list
- Quote detail
- Quote create
- Quote item editor
- Issue confirmation
- Accept/reject action
- Rejection reason dialog
- WorkOrder conversion confirmation

Quote state görünümü:

```text
Draft -> Issued -> Accepted
                 -> Rejected
                 -> Expired
```

### 4.5 Work Orders

Ekranlar:

- WorkOrder list
- Technician assigned list
- WorkOrder detail
- Assignment dialog
- Start/complete action
- Completion summary
- Related quote/payment/audit timeline

Technician ekranı yalnızca `AssignedUserId` kendi user id’si olan işleri göstermelidir.

### 4.6 Inventory

Ekranlar:

- Stock list
- Material detail
- Adjustment dialog
- Reservation dialog
- Movement history
- Low-stock view

Her adjustment için kullanıcıya şu özet gösterilir:

```text
Previous quantity
+/- Delta
New quantity
Reason
Actor
Operation id
```

### 4.7 Payments

Ekranlar:

- Customer payment list
- Create payment
- Invoice allocation
- Customer ledger
- Allocation conflict
- Over-allocation warning

### 4.8 Audit / Operations

Ekranlar:

- Operation list
- Operation detail
- Entity timeline
- Before/after snapshot viewer
- Outbox queue
- Dead-letter list
- Replay confirmation

Hassas değerler frontend'de de maskeli gösterilir.

### 4.9 Admin

Ekranlar:

- Pending users
- User approval
- Role assignment
- Session revoke
- Reference values
- System health
- Metrics link

## 5. API ekran bağlantıları

| Ekran | API yüzeyi |
|---|---|
| Login | `/api/auth/login` |
| Register | `/api/auth/register` |
| Pending approval | `/api/auth/register` sonucu `202` |
| User approval | `/api/auth/users/{id}/approve` |
| Role assignment | `/api/auth/users/{id}/roles` |
| Session revoke | `/api/auth/session/revoke` |
| Password reset | `/api/auth/password-reset/request`, `/complete` |
| Customer list | `/api/customers` |
| Candidate create | `/api/customers/candidates` |
| Customer conversion | `/api/customers/candidates/{id}/convert` |
| Quote list | `/api/quotes?customerId={id}` |
| Quote item | `/api/quotes/{id}/items` |
| Quote lifecycle | `/issue`, `/accept`, `/reject` |
| Quote conversion | `/api/quotes/{id}/work-order` |
| WorkOrder | `/api/workorders` |
| Assignment | `/api/workorders/{id}/assign` |
| Inventory | `/api/inventory` |
| Projects | `/api/projects` |
| Billing | `/api/billing` |
| Payments | `/api/payments` |
| Operations | `/api/operations` |

## 6. Request metadata standardı

Frontend her mutation request'inde mümkün olduğunca şu header'ları göndermelidir:

```http
Authorization: Bearer <token>
X-Client-Screen: WorkOrderDetail
X-Client-Action: CompleteWorkOrder
X-Parent-Operation-Id: <parent-operation-id>
Idempotency-Key: <stable-request-key>
```

Kurallar:

- Retry edilebilir create/mutation işlemlerinde aynı `Idempotency-Key` korunur.
- Yeni kullanıcı aksiyonunda yeni key üretilir.
- `X-Operation-Id` destek ve audit ekranlarında gösterilebilir.
- Raw request body frontend loglarına yazılmaz.

## 7. Problem Details UX standardı

Frontend hata response'larını `status` ve `type` alanlarına göre işler.

| HTTP/type | UX davranışı |
|---|---|
| 400 | Form alanı veya request hatası göster |
| 401 | Login ekranına yönlendir |
| 403 | Yetki mesajı ve görünür aksiyonları güncelle |
| 404 | Kaydın bulunamadığını göster |
| 409 | Conflict çözüm ekranı ve refresh/retry öner |
| 422 | Alan/business validation mesajları göster |
| 429 | Retry-After bilgisini kullan |
| 503 | Sistem/altyapı uyarısı, otomatik retry kontrollü |
| 500 | Generic hata + operation id ile destek mesajı |

## 8. Component inventory

### Layout

- App shell
- Sidebar
- Top bar
- Breadcrumb
- Page header
- Content panel

### Data

- Data table
- Filter bar
- Search input
- Sort control
- Pagination
- Status badge
- Empty state
- Skeleton loading

### Workflow

- Confirm dialog
- Form drawer
- Stepper
- Timeline
- Assignment picker
- Approval panel
- Conflict dialog
- Retry action

### Observability

- Operation id copy/view
- Audit timeline
- Before/after diff
- Outbox status badge
- Dead-letter replay dialog

## 9. Visual language

İlk tasarım yönü:

- Sessiz, operasyonel, profesyonel
- Açık yüzeyler ve yüksek okunabilirlik
- Durum renkleri anlamlı ve sınırlı
- Kırmızı yalnız hata/tehlike için
- Yeşil yalnız başarı/aktif durum için
- Mavi bilgi/primary action için
- Sarı bekleyen/uyarı için
- Kartlar yalnız gerçek tekrar eden öğeleri gruplamak için
- Dashboard dekoratif hero yerine doğrudan operasyon özeti olarak çalışır

## 10. Responsive davranış

Desktop:

- Sol navigasyon
- Yoğun tablo
- Sağ panel/drawer detayları

Tablet:

- Daraltılabilir navigasyon
- Tablo kolonlarının önceliklendirilmesi
- Drawer tam ekran olabilir

Mobile/field:

- Technician iş listesi
- Büyük durum ve aksiyon butonları
- Hızlı tamamlama / hızlı durum güncelleme

## 11. Evrensel UX & Bağlam Koruma (Context Preservation) Standartları

Operatörlerin zihinsel haritasını, filtrelerini ve tablo bağlamını kaybetmemesi için süreç başlatma ve detay inceleme kararları aşağıdaki standartlara bağlıdır:

### 11.1 Karar Matrisi

| Senaryo / İşlem Tipi | Desen | Tercih Nedeni & Davranış Kriteri |
|---|---|---|
| **Hızlı, Kısa Veri Girişi** (Örn: Yeni müşteri, yeni teklif, yeni iş emri) | **Center Modal (Dialog)** | 3-5 alanı geçmeyen, kullanıcının dikkatini geçici olarak toplayan odaklanmış pencereler. |
| **Bağlamlı / Karşılaştırmalı İnceleme** (Örn: İş emri detayı, müşteri geçmişi, stok hareketleri) | **Side Drawer (Slide-over)** | Ana liste solda görünür kalır; operatör arkadaki tabloya referans vererek sağ panelde detayları inceler veya aksiyon alır. |
| **Kritik / Geri Alınamaz Aksiyonlar** (Örn: Kullanıcı yetkisi alma, oturum iptali, teklif reddi) | **Destructive Alert Modal** | Arka plan karartması (scrim) ve net uyarı/onay adımı ile riski izole eder. |
| **Tek Satır / Hızlı Müdahaleler** (Örn: Teknisyen atama, durum değiştirme) | **Inline Action / Popover** | Sayfa değiştirmeden ve modal açmadan doğrudan satır üzerinde sürtünmesiz etkileşim. |

### 11.2 Ergonomi, Güvenlik ve Erişilebilirlik Kuralları

1. **Backdrop Scrim (Arka Plan Karartması):** Modal ve Drawer açıldığında arka plan %40-%60 opaklıkta karartılarak odak sağlanır, arka plan tıklanabilirliği engellenir ancak bağlam görünür kalır.
2. **Klavye & Odak Yönetimi (Focus Trap & ESC):** Açılan her katmanda `Tab` odağı katman içinde tutulur, ilk geçerli alana `autoFocus` verilir ve `Escape` tuşu ile kapatma desteklenir.
3. **Dirty State Guard (Kirli Durum Koruması):** Kullanıcı form üzerinde değişiklik yapmışsa arka plana yanlışlıkla tıklaması veya ESC basması durumunda onay istenmeden form kapatılmaz.
4. **Scroll Lock:** Modal veya Drawer açıkken arka plandaki sayfa kaydırması kilitlenir (`overflow: hidden`).
5. **RFC 9457 Problem Details Entegrasyonu:** Tüm hata bildirimleri katmanın içinde doğrudan gösterilir; `title`, `detail` ve `traceId` alanları korunur.
- Not ekle
- Stok kullanımı
- Offline desteği sonraki faz

## 11. Ekran geliştirme sırası

1. App shell ve authentication
2. Dashboard
3. Customers/candidate conversion
4. Quotes ve quote item editor
5. WorkOrders ve assignment
6. Technician mobile workflow
7. Inventory
8. Payments/ledger
9. Admin approval/roles
10. Audit/operations
11. Metrics/dead-letter operations

## 12. Frontend backlog'unda sonraki kararlar

Frontend koduna geçmeden önce şu kararlar ayrıca kilitlenmelidir:

- Frontend framework ve build tool
- State management yaklaşımı
- Server-state/cache yaklaşımı
- Form validation kütüphanesi
- Design token/font seçimi
- Table/pagination standardı
- Authorization route guard davranışı
- Token/session saklama stratejisi
- Refresh/retry davranışı
- Accessibility hedefi
- Error tracking ve client log politikası

## 13. Tamamlanma kriterleri

Bir ekran tamamlanmış sayılmaz; şu durumları da göstermelidir:

- Loading
- Empty
- Success
- Validation error
- Unauthorized
- Forbidden
- Not found
- Conflict
- Rate limit
- Network failure
- Retry
- Operation id/audit linki

Bu blueprint, frontend implementation başlamadan önceki temel UI/UX çalışma alanıdır. Ekranlar bu sıra ile tek tek detaylandırılmalı ve her ekran API kullanım kılavuzundaki sözleşmeye bağlanmalıdır.
