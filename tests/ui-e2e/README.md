# Voltflow End-to-End (UI-E2E) Browser Test Paketi

Bu klasör; Voltflow web uygulamasının kullanıcı arayüzünü, form validasyonlarını, modal ve drawer deneyimini, durum makinesi (FSM) geçişlerini doğrudan **Chromium/Chrome** üzerinde doğrulayan **Playwright (`.spec.ts`)** test otomasyon paketidir.

Mimarisi; **SOLID / Tek Sorumluluk Prensibi (SRP)** ve **Voltflow Unified Taxonomy (VUT: `MM.F.SS`)** standardında **1 Dosya = 1 Senaryo** olarak 1-e-1 izlenebilir (traceable) yapılandırılmıştır.

---

## 📁 27 Vitrin (Showcase) UI-E2E Senaryo Hiyerarşisi (27 Dosya / 27 Senaryo)

```text
tests/ui-e2e/
├── 01-IdentityAndAuthFlows/
│   ├── 01-LoginAndApproval/
│   │   ├── 01_101_login_unapproved_or_wrong_password.spec.ts   (Senaryo [01.1.01]: Hatalı şifre / geçersiz hesap giriş engeli)
│   │   ├── 01_102_login_unapproved_pending.spec.ts             (Senaryo [01.1.02]: Onay bekleyen hesap geri bildirimi)
│   │   └── 01_201_register_with_master_otp.spec.ts             (Senaryo [01.2.01]: Master OTP 000000 ile anında onaylı kayıt & dashboard)
│   └── 02-SessionManagement/
│       ├── 01_301_logout_and_session_revocation.spec.ts       (Senaryo [01.3.01]: Oturum iptali / Revoke ve güvenli çıkış)
│       ├── 01_302_unauthenticated_route_guard.spec.ts         (Senaryo [01.3.02]: Oturumsuz erişim koruması)
│       └── 01_401_password_reset_with_otp.spec.ts             (Senaryo [01.4.01]: Master OTP ile şifre sıfırlama)
│
├── 02-QuoteToOrderFlows/
│   ├── 01-CustomerAndSites/
│   │   ├── 02_101_create_customer_modal.spec.ts                (Senaryo [02.1.01]: Müşteri oluşturma ve aktivasyon modalı)
│   │   ├── 02_102_create_customer_validation_errors.spec.ts   (Senaryo [02.1.02]: Müşteri form validasyon engelleri)
│   │   └── 02_201_customer_sites_and_assets.spec.ts           (Senaryo [02.2.01]: Müşteri tesis ve ekipman varlık yönetimi)
│   └── 02-QuoteLifecycle/
│       ├── 02_301_empty_quote_issue_blocked.spec.ts           (Senaryo [02.3.01]: Kalemsiz teklif yayınlama engeli)
│       ├── 02_401_quote_totals_and_deposit_accept.spec.ts     (Senaryo [02.4.01]: Kalem toplamı, teklif kabulü ve %30 peşinat)
│       └── 02_501_convert_quote_to_work_order.spec.ts         (Senaryo [02.5.01]: Kabul edilen tekliften iş emri üretimi)
│
├── 03-WorkOrderExecutionFlows/
│   ├── 01-TechnicianDispatch/
│   │   └── 03_051_technician_dispatch_and_assignment.spec.ts  (Senaryo [03.0.51]: Saha sevk, teknisyen atama ve filtreleme)
│   ├── 02-ExecutionAndSafety/
│   │   ├── 03_101_safety_checklist_mandatory_gate.spec.ts     (Senaryo [03.1.01]: İSG güvenlik kontrol listesi onay kilidi)
│   │   ├── 03_201_technician_check_in_and_timer.spec.ts       (Senaryo [03.2.01]: Saha check-in ve canlı çalışma sayacı)
│   │   └── 03_301_work_order_put_on_hold_checkout.spec.ts     (Senaryo [03.3.01]: On-hold ve otomatik check-out telafisi)
│   └── 03-CompletionAndReview/
│       ├── 03_401_complete_work_order_with_proof.spec.ts      (Senaryo [03.4.01]: Kanıtlı tamamlama - imza & fotoğraf)
│       └── 03_501_manager_approve_for_billing.spec.ts         (Senaryo [03.5.01]: Yönetici ofis onayı - Invoiced terminal durumu)
│
├── 04-InventoryFlows/
│   ├── 01-StockManagement/
│   │   └── 04_101_negative_stock_blocked_rollback.spec.ts     (Senaryo [04.1.01]: Eksi stok engeli ve form validasyonu)
│   └── 02-Reservations/
│       └── 04_201_material_reservation_available.spec.ts      (Senaryo [04.2.01]: İş emrine stok rezervasyonu & bakiye)
│
├── 05-FinanceAndLedgerFlows/
│   ├── 01-InvoicingAndBilling/
│   │   ├── 05_101_invoice_generation_deposit_deduction.spec.ts (Senaryo [05.1.01]: Fatura üretimi ve peşinat mahsubu)
│   │   └── 05_201_customer_payment_ledger_credit.spec.ts      (Senaryo [05.2.01]: Tahsilat girişi ve cari hesap defteri alacağı)
│   └── 02-PaymentAllocations/
│       └── 05_301_allocate_payment_to_invoice.spec.ts         (Senaryo [05.3.01]: Tahsilatın faturaya dağıtımı ve aşım engeli)
│
└── 06-LocalizationAndSystemFlows/
    ├── 06_101_i18n_language_switcher_and_localization.spec.ts (Senaryo [06.1.01]: Halka 17 — Auth ekranı EN/TR dil seçimi)
    ├── 06_102_dashboard_i18n_language_switcher.spec.ts        (Senaryo [06.1.02]: Halka 17 — Dashboard EN/TR navigasyon yerelleştirmesi)
    ├── 06_201_fsm_state_transitions_visibility.spec.ts        (Senaryo [06.2.01]: Halka 18 — Teklif FSM durum geçişleri)
    └── 06_202_workorder_fsm_state_transitions.spec.ts         (Senaryo [06.2.02]: Halka 18 — İş Emri FSM durum geçişleri)
```

---

## 🚀 Testleri Çalıştırma

Playwright testlerini çalıştırmak için proje ana dizininde:

```bash
# Tüm UI-E2E testlerini headless Chromium ile koşmak için:
npx playwright test

# Yalnızca belirli bir modül testlerini koşmak için:
npx playwright test tests/ui-e2e/01-IdentityAndAuthFlows/
npx playwright test tests/ui-e2e/02-QuoteToOrderFlows/
npx playwright test tests/ui-e2e/03-WorkOrderExecutionFlows/
npx playwright test tests/ui-e2e/04-InventoryFlows/
npx playwright test tests/ui-e2e/05-FinanceAndLedgerFlows/

# Testleri canlı tarayıcı arayüzünde (Headed mode) izleyerek koşmak için:
npx playwright test --headed

# Test adımlarının listesini görüntülemek için:
npx playwright test --list
```

---

## 📖 Mimari Senkronizasyon Referansları
* 📘 [Voltflow Kapsamlı Dry-Test Rehberi](../../docs/architecture/voltflow-dry-tests-guide.md)
* 📗 [Voltflow İş Akışları ve Hata Modları](../../docs/architecture/voltflow-workflows-and-failure-modes.md)
* 📙 [Master VUT Taxonomy](../../src/Voltflow.Domain/Common/VoltflowTaxonomy.cs)
* 📕 [API-E2E Test Kütüphanesi](../api-e2e/README.md)
* 📒 [Backend C# Test Katmanı](../backend/README.md)
