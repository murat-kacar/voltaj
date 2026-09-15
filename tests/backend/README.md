# Voltflow Backend C# Test Katmanı (12. Halka)

Bu klasör; Voltflow çekirdek sisteminin iş mantığını, etki alanı kurallarını (Domain Invariants), CQRS servis orkestrasyonunu, işlem altyapısını ve mimari kurallarını xUnit, Moq ve WebApplicationFactory kullanarak doğrulayan **117 testlik kurumsal C# test paketini** barındırır.

Voltflow Unified Taxonomy (VUT) mimarisinde **12. Halka** olarak mühürlenmiştir.

---

## 📁 Katmanlar ve VUT Test Hiyerarşisi

```text
tests/backend/
├── 01-Domain/                          (4 dosya - Saf Etki Alanı & FSM Kuralları)
│   ├── Finance/FinanceTests.cs         (Modül 05: Fatura, cari hesap ve tahsisat kuralları)
│   ├── Inventory/InventoryTests.cs     (Modül 04: Eksi stok sınırı ve rezervasyon invariant'ları)
│   ├── Quotes/QuoteTests.cs            (Modül 02: Teklif kalem toplamı, %30 peşinat ve FSM geçişleri)
│   └── WorkOrders/WorkOrderTests.cs    (Modül 03: İSG onayı, teknisyen atama, tamamlama kuralları)
│
├── 02-Application/                     (3 dosya - CQRS & Moq İzole Birim Testleri)
│   ├── AuthServiceTests.cs             (Modül 01: Login, OTP kayıt, şifre sıfırlama, oturum iptali)
│   ├── InventoryServiceTests.cs        (Modül 04: Stok sayım, düşüm, rezervasyon servisi mantığı)
│   └── WorkOrderServiceTests.cs        (Modül 03: İş emri oluşturma, teknisyen atama, check-in servisi)
│
├── 03-Infrastructure/                  (3 dosya - Altyapı & Dayanıklılık Testleri)
│   ├── ConcurrencyAndIdempotencyTests.cs (Modül 08: Optimistic concurrency & Idempotency guard)
│   ├── OutboxDrainTests.cs             (Modül 07: Olay kuyruğu tahliyesi ve transaction bütünlüğü)
│   └── OutboxProcessorTests.cs         (Modül 07: Background worker ve güvenilir olay dağıtımı)
│
├── 04-Integration/                     (13 dosya - WebApplicationFactory API Akış Testleri)
│   ├── ApiTestFixture.cs               (In-Memory SQLite, deterministic test auth & WebFactory)
│   ├── AuthIntegrationTests.cs         (Modül 01: [01.1.01] - [01.4.03] Uçtan uca kimlik testleri)
│   ├── CustomerIntegrationTests.cs     (Modül 02: [02.1.01] - [02.2.01] Müşteri & tesis API testleri)
│   ├── QuoteIntegrationTests.cs        (Modül 02: [02.3.01] - [02.5.01] Tekliften iş emrine API testleri)
│   ├── WorkOrderIntegrationTests.cs    (Modül 03: [03.1.01] - [03.5.01] Saha iş emri FSM API testleri)
│   ├── InventoryIntegrationTests.cs    (Modül 04: [04.1.01] - [04.2.01] Depo & stok API testleri)
│   ├── FinanceIntegrationTests.cs      (Modül 05: [05.1.01] - [05.3.01] Fatura & tahsilat API testleri)
│   ├── FinanceAndInventoryEdgeTests.cs (Modül 04/05: Uç sınır durumları ve rollback testleri)
│   ├── HealthIntegrationTests.cs       (Sistem sağlık izleme ve readiness/liveness testleri)
│   ├── IsolationAndRbacTests.cs        (Modül 01/08: Çoklu rol yetkilendirme ve veri izolasyonu)
│   ├── ReminderIntegrationTests.cs     (Modül 07: Hatırlatıcı ve zamanlanmış görev testleri)
│   ├── TestAuthHelper.cs               (Test JWT token üretici yardımcı sınıf)
│   └── TimeTravelingTests.cs           (Modül 07/08: Zaman bükme ve periyodik sözleşme tetikleme)
│
└── 05-Architecture/                    (2 dosya - ArchUnitNET ve Negatif Matris Testleri)
    ├── ArchitectureRefactorTests.cs    (Katman bağımlılık kuralları, Guard kontrolleri, Outbox standartları)
    └── FsmNegativeMatrixTests.cs       ($N \times N$ Tüm geçersiz durum geçişlerinin engellendiğinin kanıtı)
```

---

## 🚀 Testleri Çalıştırma

```bash
# Tüm 117 C# testini çalıştırmak için:
dotnet test tests/backend

# Detaylı çıktı ile koşmak için:
dotnet test tests/backend --logger "console;verbosity=normal"

# Yalnızca belirli bir katmanı koşmak için:
dotnet test tests/backend --filter "FullyQualifiedName~Integration"
dotnet test tests/backend --filter "FullyQualifiedName~Domain"
dotnet test tests/backend --filter "FullyQualifiedName~Application"
dotnet test tests/backend --filter "FullyQualifiedName~Architecture"
```

---

## 📖 12-Halka Mimari Senkronizasyon Referansları
* 📘 [Voltflow Kapsamlı Dry-Test Rehberi](../../docs/architecture/voltflow-dry-tests-guide.md)
* 📗 [Voltflow İş Akışları ve Hata Modları](../../docs/architecture/voltflow-workflows-and-failure-modes.md)
* 📙 [Master VUT Taxonomy](../../src/Voltflow.Domain/Common/VoltflowTaxonomy.cs)
* 📕 [API-E2E Test Kütüphanesi](../api-e2e/README.md)
* 📓 [UI-E2E Test Paketi](../ui-e2e/README.md)
