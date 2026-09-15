# Voltflow İlk Sprint Planı

Bu belge, sistemin ilk çalışan, iş değerini üreten versiyonunu üretmek için en kritik önceliklerin listesini sunar.

## 1. Amaç

İlk sprintte, kullanıcıların temel iş akışını görebileceği ve kullanabileceği bir çekirdek yapı kurmak gerekir.

Temel iş akışı:

- Müşteri adayı oluşturma
- Müşteri dönüştürme
- Teklif oluşturma ve yayınlama
- İş emri oluşturma
- Malzeme / işçilik ekleme
- Teklif ve iş emri yöneticisi akışı
- Stok güncellemesi
- Fatura oluşturma
- Tahsilat takibi
- Uyarı ve hatırlatma sistemi

## 2. Sprint hedefleri

### Hedef 1: Customer lifecycle
- CandidateCustomer oluşturma
- Customer conversion
- Customer read/edit flows
- Audit log
- Basic party identity model

### Hedef 2: Quote lifecycle
- Quote create
- Quote item add
- Quote issue
- Quote accept / reject / expire
- Quote totals validation

### Hedef 3: Work order lifecycle
- WorkOrder create
- WorkOrder assignment
- Material and labor entries
- Completion
- Invoice linkage

### Hedef 4: Inventory management
- Material master
- Stock balances
- Stock movements
- Low stock alert

### Hedef 5: Finance basics
- Payment create
- Payment allocation to invoice
- Customer ledger update
- Basic financial reports

### Hedef 6: Notifications
- Reminder creation
- Due payment reminders
- Overdue warning
- Email / document delivery hook

## 3. Öncelik sırası

### P0 – Acil ve kritik
1. Customer creation and conversion
2. Quote create and issue
3. WorkOrder create and completion
4. Stock movement and balance update
5. Payment allocation and ledger

### P1 – İşletim ve operasyon
6. Reminder system
7. Document generation
8. Export pipeline
9. Role and permission policy
10. Audit and observability

### P2 – Gelişmiş
11. Reporting dashboard
12. Project billing flow
13. Advanced approvals
14. Analytics and KPIs

## 4. Uygulama katmanları

### API
- customer endpoints
- quote endpoints
- work-order endpoints
- stock endpoints
- payment endpoints
- reminder endpoints

### Application
- commands and handlers
- DTOs
- validators
- policies

### Domain
- aggregate roots
- invariants
- domain events
- state transitions

### Infrastructure
- PostgreSQL repository layer
- EF Core mappings
- Hangfire jobs
- MinIO adapter
- email provider adapter
- PDF renderer adapter

## 5. İlk sprintte üretilecek iş akışları

### Workflow A: Müşteri adayından müşteri oluşturma
1. candidate customer oluştur
2. müşteri doğrulama / review
3. conversion işlemi
4. party and customer master row oluşur
5. audit log kaydı oluşur

### Workflow B: Tekliften iş emrine
1. quote oluştur
2. item ekle
3. issue
4. accept by customer
5. work order oluştur
6. assignment yap
7. completion sonrası fatura bağla

### Workflow C: Stok ve finans senaryosu
1. stock movement gir
2. balance güncelle
3. ledger oluştur
4. payment gir
5. allocation yap
6. customer balance kontrol et

## 6. Kritik başarı kriterleri

- Teklif ve iş emri akışı tam çalışır olmalı.
- Stok / finans / ledger update tek transaction içinde gerçekleşmeli.
- Hata durumunda rollback gerçekleşmeli.
- Tüm kritik işlerde actor ve timestamp kayıtlı olmalı.
- Role-based access açıkça tanımlı olmalı.
- Reminder ve export iş akışları background job olarak çalışmalı.

## 7. İlk sprint sonunda beklenen çıktılar

- Müşteri yönetimi tamamlandı
- Quote lifecycle çalışıyor
- Work order lifecycle çalışıyor
- Stok balance ve movement ledger temel akışı çalışıyor
- Payment, customer ledger ve invoice allocation temel akışı çalışıyor
- Reminder persistence, retry lifecycle ve Worker polling çalışıyor; external delivery provider entegrasyonu deferred
- Minimal dashboard ve report ekranları hazır

## 8. Son not

Bu ilk sprint, tek firma için “şema + davranış + iş akışı” temellerini kurar. Bu aşama başarılı olursa sonraki aşamada reporting, automation ve operasyonel iyileştirmeler geliştirilir.
