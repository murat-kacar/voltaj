# Voltflow API Kullanim Kilavuzu

Bu belge mevcut backend endpoint sozlesmesini, yetkileri, veri etkilerini ve kullanici ekran akisini anlatir. Endpoint davranisi kodla birlikte degisebilir; uygulama degistiginde bu belge guncellenmelidir.

## 1. Calisma ortami

API Docker ile `http://localhost:8080` adresinde calisir.

Saglik endpoint'leri:

```text
GET /health
GET /ready
GET /startup
GET /metrics
```

- `/health`: API prosesinin ayakta oldugunu gosterir (liveness; hicbir bagimliligi kontrol etmez).
- `/ready`: API'nin PostgreSQL'e baglanabildigini gosterir (readiness).
- `/startup`: bir kerelik baslangic isinin bittigini ve veritabani semasinda bekleyen migration olmadigini gosterir; o zamana kadar `503` (`starting` / `migrations_pending`) doner.
- `/metrics`: Prometheus uyumlu temel HTTP sayaçlarini dondurur.

## 2. Kimlik ve ortak header'lar

Korumali endpoint'lerde:

```http
Authorization: Bearer <jwt>
X-Client-Screen: CustomerDetail
X-Client-Action: ConvertCustomer
X-Parent-Operation-Id: <operation-id>
Idempotency-Key: <unique-key>
```

Her response'ta basarili request'lerde `X-Operation-Id` bulunur.

### Roller

| Rol | Yetki |
|---|---|
| `Admin` | Tum yonetim, approval, role assignment, operasyon ve raporlama |
| `Manager` | Musteri, teklif, proje, stok, fatura ve operasyon yazma islemleri |
| `Technician` | Operasyonel work order ve stok islemleri; yalniz atandigi WorkOrder okuma kapsami |
| `Viewer` | Salt-okuma |

Yeni kullanici once pending durumundadir. Admin approval sonrasi ilk rol `Viewer` atanir.

## 3. Hata standardi

Hatalar RFC 9457 Problem Details formatindadir:

```json
{
  "type": "https://voltflow.dev/problems/business-validation",
  "title": "Business validation failed",
  "status": 422,
  "detail": "...",
  "instance": "/api/...",
  "traceId": "...",
  "operationId": "..."
}
```

Temel kodlar:

| HTTP | Kod/Anlam |
|---:|---|
| 400 | Invalid argument |
| 401 | Authentication gerekli/gecersiz |
| 403 | Role yetkisi yok |
| 404 | Kayit bulunamadi |
| 409 | Business rule veya concurrency conflict |
| 422 | Application validation |
| 429 | Rate limit asildi |
| 500 | Beklenmeyen hata |
| 503 | Redis/rate-limit altyapisi kullanilamiyor |

## 4. Auth endpoint'leri

### Register

```http
POST /api/auth/register
```

Request:

```json
{
  "name": "Ali Veli",
  "email": "ali@example.com",
  "password": "StrongPassword123!"
}
```

Sonuc: `202 Accepted`. Kullanici token alamaz; Admin approval bekler.

Yazilan temel tablolar:

- `AppUsers`
- Audit: `AuditEvents`
- Outbox: `OutboxMessages`
- Operation: `OperationTraces`

### Login

```http
POST /api/auth/login
```

Onaylanmis kullaniciya JWT verir ve session hash'i `UserSessions` tablosuna yazar.

### Admin approval

```http
POST /api/auth/users/{userId}/approve
```

Yetki: `Admin`.

Etkiler:

- `AppUsers.IsApproved = true`
- Ilk `Viewer` role kaydi `AppUserRoles` tablosuna eklenir.

### Role assignment

```http
POST /api/auth/users/{userId}/roles
```

Request:

```json
{
  "roleName": "Technician"
}
```

Yetki: `Admin`.

### Session revoke

```http
POST /api/auth/session/revoke
```

Request:

```json
{
  "token": "<jwt>"
}
```

Raw token database'e yazilmaz; `UserSessions.TokenHash` saklanir. Revoke sonrasi JWT authentication reddedilir.

### Password reset

```http
POST /api/auth/password-reset/request
POST /api/auth/password-reset/complete
```

Token hash'i `PasswordResetTokens` tablosunda tutulur ve tek kullanimlidir. External email/SMS provider bu kurulumun disinda oldugu icin reset token teslimati ayrica provider entegrasyonu gerektirir.

## 5. Customer endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/customers` | Authenticated | Musterileri listeler |
| GET | `/api/customers/{id}` | Authenticated | Musteri detayi |
| POST | `/api/customers` | Admin/Manager | Dogrudan customer olusturur |
| POST | `/api/customers/candidates` | Admin/Manager | Candidate customer olusturur |
| POST | `/api/customers/candidates/{id}/convert` | Admin/Manager | Candidate'i tek seferlik customer'a cevirir |

Conversion etkileri:

- `CandidateCustomers`
- `Customers`
- `CustomerConversions`
- Audit/outbox/operation trace

Conversion `OnceEver` ve idempotency korumalidir.

## 6. Quote endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/quotes?customerId={id}` | Authenticated | Quote listesi |
| GET | `/api/quotes/{id}` | Authenticated | Quote detayi |
| POST | `/api/quotes` | Admin/Manager | Draft quote olusturur |
| POST | `/api/quotes/{id}/items` | Admin/Manager | Quote item ekler |
| POST | `/api/quotes/{id}/issue` | Admin/Manager | Quote publish/issue |
| POST | `/api/quotes/{id}/accept` | Admin/Manager | Quote kabul |
| POST | `/api/quotes/{id}/reject` | Admin/Manager | Reason ile reject |
| POST | `/api/quotes/{id}/work-order` | Admin/Manager | Accepted quote'tan WorkOrder |

Reject request:

```json
{
  "reason": "Customer declined the offer."
}
```

Quote item request:

```json
{
  "description": "Installation",
  "quantity": 2,
  "unitPrice": 150
}
```

Yazilan temel tablolar:

- `Quotes`
- `QuoteItems`
- `WorkOrders`
- `WorkOrderItems`
- Audit/outbox/operation trace

Quote-to-WorkOrder yalniz accepted quote icin gecerlidir ve `OnceEver` davranisi tasir.

## 7. WorkOrder endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/workorders` | Admin/Manager/Technician | Liste; Technician yalniz atandigi isleri gorur |
| GET | `/api/workorders/{id}` | Admin/Manager/Technician | Detay; Technician assignment scope uygulanir |
| POST | `/api/workorders` | Admin/Manager/Technician | WorkOrder olusturur |
| POST | `/api/workorders/{id}/assign` | Admin/Manager/Technician | Employee user'a atar |
| POST | `/api/workorders/{id}/complete` | Admin/Manager/Technician | Tamamlar |

Assignment request:

```json
{
  "employeeUserId": "guid"
}
```

WorkOrder verisi `WorkOrders`, item'lar `WorkOrderItems` tablolarinda tutulur.

## 8. Inventory endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/inventory/{materialCode}` | Admin/Manager/Technician | Stok detayi |
| POST | `/api/inventory/adjust` | Admin/Manager/Technician | Transactional stock adjustment |
| POST | `/api/inventory/reserve` | Admin/Manager/Technician | Stock reservation |

Adjustment request:

```json
{
  "materialCode": "MAT-001",
  "delta": 5
}
```

Adjustment balance ve append-only `StockMovements` kaydini ayni transaction'da yazar.

## 9. Project/Billing endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/projects` | Authenticated | Proje listesi |
| GET | `/api/projects/{id}` | Authenticated | Proje detayi |
| POST | `/api/projects` | Admin/Manager | Proje olusturur |
| POST | `/api/projects/{id}/phases` | Admin/Manager | Phase ekler |
| GET | `/api/billing/{projectId}` | Authenticated | Billing listesi |
| POST | `/api/billing/{projectId}` | Admin/Manager | Billing entry olusturur |

## 10. Payment endpoint'leri

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/payments/{customerId}` | Authenticated | Payment listesi |
| POST | `/api/payments` | Admin/Manager | Payment ve customer ledger kaydi |
| POST | `/api/payments/allocate` | Admin/Manager | Payment'i invoice'a allocate eder |

Payment allocation payment ve invoice remaining amount'ini asamaz; duplicate allocation reddedilir.

## 11. Admin operations

| Method | Path | Yetki | Islev |
|---|---|---|---|
| GET | `/api/operations/outbox/dead-letter` | Admin | Dead-letter mesajlari |
| POST | `/api/operations/outbox/{id}/replay` | Admin | Dead-letter mesajini yeniden kuyruğa alir |

## 12. Persistence ve logging etkisi

Bir mutation endpoint'i normalde su katmanlari etkiler:

1. Domain aggregate/entity
2. Ilgili EF tablosu
3. `AuditEvents`
4. `OperationTraces`
5. `OutboxMessages`
6. Gerekirse `ExecutionGuards`
7. Gerekirse `UserSessions`, `PasswordResetTokens` veya `StockMovements`

Audit snapshot'lari email, telefon, vergi numarasi, adres, password, token ve secret alanlarini maskeler.

## 13. Kullanimda dikkat edilecekler

- Mutation'larda `Idempotency-Key` kullanin.
- Ekran ve buton takibi icin `X-Client-Screen` ve `X-Client-Action` gonderin.
- Alt operasyonlari baglamak icin `X-Parent-Operation-Id` kullanin.
- `X-Operation-Id` degerini destek taleplerinde saklayin.
- Admin approval olmadan register kullanicisi business endpoint'lerine erisemez.
- Technician yalniz kendisine atanmis WorkOrder'lari gorur.
- Rate limit Redis uzerinden endpoint + user/IP partition ile uygulanir.

## 14. Eklenmesi faydali sonraki yuzeyler

External provider kapsam disinda, sonraki faydali urun yuzeyleri:

- Admin pending-user listesi ve approval ekrani
- Role removal/deactivation
- WorkOrder assignment history
- Audit/operation timeline ekrani
- Dead-letter operasyon paneli
- Metrics dashboard
- Backup/restore yonetim paneli
- Redis rate-limit dashboard'u
- Pagination, sorting, filtering ve export endpoint'leri
- API versioning ve OpenAPI security schemes
