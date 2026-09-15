# Voltflow Unified Taxonomy (VUT) Numaralandırma Mantığı ve Tam Kod Kataloğu

Bu doküman; Voltflow mimarisindeki **Voltflow Unified Taxonomy (VUT: `MM.F.SS`)** standartlarının **numara üretme algoritmasını**, **18 halkadaki kodlama ve türetme kurallarını** ve şu anda sistemde aktif olan **96 senaryonun eksiksiz kanonik kataloğunu** belgeler.

---

## 🎯 1. Numaralandırma Mimarisi ve Felsefesi

Voltflow kurumsal mimarisinde **"Zero-Ambiguity (Sıfır Belirsizlik)"** ilkesi geçerlidir. Canlı sistemde, log kayıtlarında veya test koşumlarında bir kodla karşılaşıldığında (`01_103`, `VF-01101`, `ACT-01103` vb.), geliştirici ve QA mühendisi **hiçbir tahmin yürütmeden doğrudan:**

1. İlgili mimari doküman maddesine (`docs/`),
2. İlgili C# backend servis ve endpoint dosyasına (`src/`),
3. İlgili React UI bileşeni ve form alanına (`frontend/src/`),
4. İlgili xUnit backend testine (`tests/backend/`),
5. İlgili PowerShell API testine (`tests/api-e2e/`),
6. İlgili Playwright browser testine (`tests/ui-e2e/`),
7. İlgili veritabanı tablolarına,
8. Graphify bilgi grafındaki AST merkezine,

tek bir komutla (`pwsh .\scripts\find-vut.ps1 <kod>`) **nokta atışı** ulaşabilmelidir.

---

## 📐 2. Kanonik Kodun Anatomisi: `[MM].[F].[SS]` (veya `MMFSS`)

Her bir operasyon ve kural **5 haneli** kanonik bir VUT kimliği taşır:

```text
   01   .   1   .   03       ->   Kanonik Kod: 01.1.03
  [MM]     [F]     [SS]      ->   Düz Kod    : 01103
   │        │       │        ->   Dosya Kodu : 01_103
   │        │       └── Senaryo / Sınır Varyasyonu No (01-99)
   │        └────────── Modül İçi Alt Akış / Faz No (1-9)
   └─────────────────── Ana Modül Kodu (01-08)
```

### Segment 1: `MM` (Ana Modül Kodu - 2 Hane)
Voltflow sisteminin ana etki alanı sınırlarını (Bounded Contexts) belirler:

| Modül Kodu (`MM`) | Modül Adı | İş Kapsamı |
| :---: | :--- | :--- |
| **`01`** | **IdentityAndAuthFlows** | Kimlik doğrulama, OTP ile kayıt, oturum iptali (revoke), şifre sıfırlama, kullanıcı onayı ve rol yönetimi. |
| **`02`** | **QuoteToOrderFlows** | Müşteri CRM (Lead -> Active), tesisler, teklif taslağı, kalem yönetimi, teklif kabulü, peşinat ve iş emrine dönüşüm. |
| **`03`** | **WorkOrderExecutionFlows** | Saha iş emirleri, İSG kontrol listesi kapısı, teknisyen atama, check-in/out zaman takibi, malzeme ekleme, kanıtlı tamamlama ve ofis onayı. |
| **`04`** | **InventoryFlows** | Depo ve malzeme yönetimi, stok düzeltme, eksi stok koruması, iş emrine malzeme rezervasyonu ve serbest bırakma. |
| **`05`** | **FinanceAndLedgerFlows** | Müşteri satış faturaları, peşinat mahsubu, tahsilat girişi, cari hesap defteri (ledger) ve tahsilatın faturaya dağıtımı. |
| **`06`** | **MaintenanceAndContractFlows** | Servis sözleşmeleri, periyodik bakım takvimi, SLA sayaçları ve otomatik bakım iş emri tetikleme (Faz 1 Worker/Altyapı entegre, Faz 2 API/UI). |
| **`07`** | **OperationsAndOutboxFlows** | Arka plan worker süreçleri, Transactional Outbox güvenilir dağıtımı, dead-letter kuyruğu ve SLA hatırlatıcıları. |
| **`08`** | **SecurityAndResilienceFlows** | Idempotency key gövde uyuşmazlığı koruması, mutation rate limiting ve RFC 7807 global hata standartları. |

### Segment 2: `F` (Alt Akış / Faz Kodu - 1 Hane)
İlgili modülün içindeki mantıksal aşamayı veya alt bileşeni tanımlar. Örneğin Modül 01 için:
- `01.1` = Giriş & Onay Kontrolleri (Login & Approval Gate)
- `01.2` = Kayıt & OTP Doğrulama (Registration & OTP)
- `01.3` = Oturum Yönetimi & İptal (Session & Revocation)
- `01.4` = Şifre Sıfırlama & Yönetici Rol Atama (Password Reset & RBAC)

### Segment 3: `SS` (Senaryo / Adım Kodu - 2 Hane)
Senaryonun türünü ve varyasyonunu belirler:
- **`01` (Ana İş Akışı / Vitrin Senaryosu - Happy Path):**
  İlgili alt fazın temel ve başarılı ana omurgasıdır. Bu kod (`.01`) **istisnasız her 16 halkada (Doküman, UI-E2E, API-E2E, Backend C#, OpenAPI, DB) 1-e-1 mevcuttur**.
- **`02`, `03`, `04`... (Sınır Durumları / Negatif Validasyon / Varyasyonlar):**
  Aynı iş akışına ait HTTP sınır durumlarını doğrular. Örneğin `01.1.01` hatalı şifreyi (401), `01.1.02` onaysız kullanıcıyı (403), `01.1.03` boş e-posta validasyonunu (400) temsil eder.

---

## 🏷️ 3. 18 Halkada Numara Türetme Standartları

Bir VUT kodu (`MM.F.SS` / `MMFSS`) belirlendiğinde, 18 halkanın her birinde aşağıdaki standart formüllerle kullanılır:

| # | Halka Adı | Format / Formül | Örnek (`01.1.03` / `02.3.01` için) |
| :---: | :--- | :--- | :--- |
| **1** | **Dokümantasyon** | `### Senaryo [MM.F.SS]: <Başlık>` | `### Senaryo [01.1.03]: Eksik Email ile Login (400)` |
| **2** | **Graphify AST** | `ps1_script:MM_FSS...` veya metot adı | `ps1_script:01_103_login_missing_email_returns_400.ps1` |
| **3** | **OpenAPI / Swagger** | `.WithName("VF-MMF00_<Ad>")` | `.WithName("VF-01101_Login")` |
| **4** | **Frontend Test-ID** | `data-testid="MMFSS-<eleman>"` | `data-testid="01103-email-error"` |
| **5** | **UI-E2E Testi** | `MM_FSS_<ad>.spec.ts` | `01_101_login_unapproved_or_wrong_password.spec.ts` |
| **6** | **API-E2E Testi** | `MM_FSS_<ad>.ps1` | `01_103_login_missing_email_returns_400.ps1` |
| **7** | **CI/CD Orkestrasyon** | `VUT=MMFSS` | `pwsh tests/run-all-tests.ps1 -Filter "01103"` |
| **8** | **RFC 7807 Hata Kodu** | `VF-MMFSS` (`VF_MMFSS`) | `VF-01103` (`VoltflowTaxonomy.ErrorCodes.VF_01103`) |
| **9** | **Log & Trace** | `SCR-MMF0` (Ekran), `ACT-MMFSS` (Aksiyon) | `SCR-0110` (Ekran), `ACT-01103` (Aksiyon) |
| **10** | **Idempotency Scope** | `SCOPE_MMFSS_<Ad>` | `SCOPE_01201_Register` |
| **11** | **Outbox EventType** | `EVT_MMFSS_<Ad>` | `EVT_01101_UserApproved` |
| **12** | **Backend C# Testi** | `[Trait("VUT", "MMFSS")]` | `[Trait("VUT", "01103")]` |
| **13** | **Deterministik Seed Data**| `SEED_MMFSS_<Varlık>` | `SEED_01101_Users` |
| **14** | **REST Client Koleksiyonu**| `# [MM.F.SS] <Method> <Route>` | `# [01.1.03] POST {{baseUrl}}/auth/login` |
| **15** | **RBAC Yetkilendirme** | `PERM_MMFSS` | `PERM_01103` |
| **16** | **Frontend Routing** | SPA Rota Yolu | `/auth/login` |
| **17** | **i18n Dil Anahtarı** | `i18n.voltflow.MM.MM_FSS` | `i18n.voltflow.01.01_103` |
| **18** | **FSM Durum Geçiş Kuralı** | Entity[StateA -> StateB] (Guard) | `Quote[Draft -> Issued] (Guard: Items.Count > 0)` |

---

### 🏛️ Gelecek Projeler İçin: 25-Halkalı Kurumsal Referans Mimarisi

İlerleyen fazlarda veya bağımsız büyük ölçekli kurumsal projelerde kullanılabilecek **25 Halkalı Kurumsal İzlenebilirlik Mimarisi**:

1. **Halka 1:** Dokümantasyon & Kabul Kriterleri (Specification / Gherkin)
2. **Halka 2:** Bilgi Grafı & AST Düğümü (Knowledge Graph / Graphify)
3. **Halka 3:** Tehdit Modellemesi & Güvenlik Standardı (OWASP ASVS / STRIDE)
4. **Halka 4:** FSM Durum Geçiş Kuralı & Guard (Finite State Machine Transitions) ⭐
5. **Halka 5:** OpenAPI / REST Endpoint & Sözleşme
6. **Halka 6:** REST Client Koleksiyonu (`requests.http`)
7. **Halka 7:** Alternatif İletişim Protokolü Şeması (GraphQL / gRPC / Protobuf)
8. **Halka 8:** API Gateway & WAF / Rate-Limit Politikası
9. **Halka 9:** Frontend Rota & Sayfa Ağacı
10. **Halka 10:** UI Bileşen & `data-testid` Ağacı
11. **Halka 11:** i18n / Çoklu Dil & Hata Mesajı Sözlüğü ⭐
12. **Halka 12:** Design System Token & Figma / Storybook
13. **Halka 13:** UI-E2E Testi (Playwright / Cypress)
14. **Halka 14:** API-E2E / Entegrasyon Testi (PowerShell / Newman)
15. **Halka 15:** Backend Birim & Entegrasyon Testi (`[Trait("VUT", ...)]`)
16. **Halka 16:** CI/CD Pipeline & Test Filtreleme Etiketi
17. **Halka 17:** RFC 7807 Hata Kodu & Domain Exception
18. **Halka 18:** Yapılandırılmış Log & Trace Eylemi (`ACT_...`, `SCR_...`)
19. **Halka 19:** RBAC Yetkilendirme & Rol İzni (`PERM_...`)
20. **Halka 20:** Idempotency Kapsamı (`SCOPE_...`)
21. **Halka 21:** Veritabanı Varlığı & Şema/Tablo
22. **Halka 22:** Deterministik Tohum Veri (Seed Data Fixtures)
23. **Halka 23:** Outbox Olay Tipi (`EVT_...`)
24. **Halka 24:** Message Broker Topic & Event Consumer (RabbitMQ / Kafka)
25. **Halka 25:** APM Metrikleri & Prometheus / Grafana Alarmları

---

## ➕ 4. Yeni Bir Senaryo / Numara Nasıl Eklenir?

Yeni bir özellik, test senaryosu veya validasyon kuralı eklendiğinde şu 4 adım izlenir:

1. **Modül (`MM`) ve Faz (`F`) Belirleyin:** Örneğin teklif kabulü için Modül `02`, Faz `4` (`02.4`).
2. **Sıradaki Senaryo Numarasını (`SS`) Alın:** `02.4` altında en son `02_407` varsa, yeni test `02_408` olur.
3. **Dosyayı Adlandırın:** `tests/api-e2e/02-QuoteToOrderFlows/02-AcceptanceAndDeposit/02_408_<aciklama>.ps1`.
4. **Matrisi Yenileyin:** `python scratch/build_traceability_system.py` çalıştırılarak hem JSON veri tabanı hem de `find-vut.ps1` otomatik senkronize edilir.

---

## 📋 5. Sistemdeki 96 Senaryonun Tam Kanonik Kataloğu (18-Halka Genişletilmiş)

### Modül [01]: Identity & Auth (19 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **01.1.01** | `01101` | Hatalı Şifre ile Giriş Reddi (401) | `01_101_wrong_password_returns_401.ps1` | `VF-01101` | `ACT-01101` | `i18n.voltflow.01.01_101` | `UserSession[None -> Failed] (Stateless Auth Guard)` |
| **01.1.02** | `01102` | Onaysız Kullanıcı Giriş Reddi (403) | `01_102_unapproved_user_returns_403.ps1` | `VF-01102` | `ACT-01102` | `i18n.voltflow.01.01_102` | `AppUser[Registered] (Guard: IsApproved == False)` |
| **01.1.03** | `01103` | Eksik Email ile Login (400) | `01_103_login_missing_email_returns_400.ps1` | `VF-01103` | `ACT-01103` | `i18n.voltflow.01.01_103` | `N/A (Stateless Validation)` |
| **01.1.04** | `01104` | Şifresiz Kayıt Denemesi (400) | `01_104_registration_missing_password_returns_400.ps1` | `VF-01104` | `ACT-01104` | `i18n.voltflow.01.01_104` | `N/A (Stateless Validation)` |
| **01.2.01** | `01201` | Master OTP (000000) ile Kayıt (200/202) | `01_201_master_otp_registration_200.ps1` | `VF-01201` | `ACT-01201` | `i18n.voltflow.01.01_201` | `AppUser[None -> Registered] (Requires Admin Approval)` |
| **01.2.02** | `01202` | Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403 | `01_202_invalid_otp_creates_unapproved_user.ps1` | `VF-01202` | `ACT-01202` | `i18n.voltflow.01.01_202` | `AppUser[None -> Unapproved] (Invalid OTP)` |
| **01.2.03** | `01203` | Mükerrer E-posta Kaydı Reddi (400/409) | `01_203_duplicate_email_registration_409.ps1` | `VF-01203` | `ACT-01203` | `i18n.voltflow.01.01_203` | `N/A (Unique Constraint Guard)` |
| **01.2.04** | `01204` | Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK) | `01_204_registered_approved_user_login_200.ps1` | `VF-01204` | `ACT-01204` | `i18n.voltflow.01.01_204` | `AppUser[Registered -> Approved]` |
| **01.4.03** | `01403` | Yönetici Kullanıcı Onayı (VF-01101) | `01_403_admin_approves_unapproved_user.ps1` | `VF-01403` | `ACT-01403` | `i18n.voltflow.01.01_403` | `AppUser[PendingApproval -> Active]` |
| **01.4.04** | `01404` | Yetkisiz Kullanıcı Onay Reddi (403) | `01_404_non_admin_approval_rejected_403.ps1` | `VF-01404` | `ACT-01404` | `i18n.voltflow.01.01_404` | `AppUser[State Unchanged] (Unauthorized)` |
| **01.4.05** | `01405` | Yönetici Tarafından Rol Ataması (VF-01101) | `01_405_assign_role_by_admin.ps1` | `VF-01405` | `ACT-01405` | `i18n.voltflow.01.01_405` | `AppUserRole[RoleAssigned]` |
| **01.4.06** | `01406` | Tanımsız Rol Atama Reddi (422) | `01_406_invalid_role_assignment_rejected_422.ps1` | `VF-01406` | `ACT-01406` | `i18n.voltflow.01.01_406` | `N/A (Stateless Role Guard)` |
| **01.4.07** | `01407` | Var Olmayan Kullanıcıyı Onaylama (404) | `01_407_approve_nonexistent_user_returns_404.ps1` | `VF-01407` | `ACT-01407` | `i18n.voltflow.01.01_407` | `N/A (Entity Not Found)` |
| **01.3.01** | `01301` | Oturum İptal Etme (204) | `01_301_session_revocation_204.ps1` | `VF-01301` | `ACT-01301` | `i18n.voltflow.01.01_301` | `UserSession[Active -> Revoked]` |
| **01.3.02** | `01302` | İptal Edilmiş Token ile Erişim (401) | `01_302_revoked_token_access_returns_401.ps1` | `VF-01302` | `ACT-01302` | `i18n.voltflow.01.01_302` | `UserSession[Revoked] (Access Blocked)` |
| **01.3.03** | `01303` | Token Olmadan Korumalı Endpoint (401) | `01_303_unauthenticated_access_returns_401.ps1` | `VF-01303` | `ACT-01303` | `i18n.voltflow.01.01_303` | `N/A (Stateless JWT Guard)` |
| **01.3.04** | `01304` | Geçersiz JWT Token (401) | `01_304_invalid_jwt_token_returns_401.ps1` | `VF-01304` | `ACT-01304` | `i18n.voltflow.01.01_304` | `N/A (Stateless JWT Guard)` |
| **01.4.01** | `01401` | Şifre Sıfırlama ve Yeni Şifre ile Giriş (200) | `01_401_password_reset_request_and_complete_200.ps1` | `VF-01401` | `ACT-01401` | `i18n.voltflow.01.01_401` | `PasswordResetToken[Pending -> Redeemed] && AppUser[PasswordUpdated]` |
| **01.4.02** | `01402` | Geçersiz Şifre Sıfırlama Token'ı (400) | `01_402_used_or_invalid_reset_token_rejected_400.ps1` | `VF-01402` | `ACT-01402` | `i18n.voltflow.01.01_402` | `PasswordResetToken[Expired/Invalid] (Rejected)` |


### Modül [02]: Quote to Order & CRM (22 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **02.1.01** | `02101` | Müşteri Lead Başlatma ve Aktivasyon | `02_101_customer_lead_to_active_conversion.ps1` | `VF-02101` | `ACT-02101` | `i18n.voltflow.02.02_101` | `Customer[Lead -> Active]` |
| **02.1.02** | `02102` | Mükerrer Müşteri Kayıt Reddi (422) | `02_102_duplicate_customer_email_rejected_422.ps1` | `VF-02102` | `ACT-02102` | `i18n.voltflow.02.02_102` | `N/A (Unique TaxId/Email Guard)` |
| **02.1.03** | `02103` | Müşteri Detay ve 404 Koruması (VF-02101) | `02_103_customer_get_by_id_and_404.ps1` | `VF-02103` | `ACT-02103` | `i18n.voltflow.02.02_103` | `N/A (Stateless Query)` |
| **02.1.04** | `02104` | Müşteri Listesi (200) | `02_104_customer_list_returns_200.ps1` | `VF-02104` | `ACT-02104` | `i18n.voltflow.02.02_104` | `N/A (Stateless Query)` |
| **02.1.05** | `02105` | Eksik Email ile Müşteri Oluşturma (422) | `02_105_create_customer_missing_email_rejected_422.ps1` | `VF-02105` | `ACT-02105` | `i18n.voltflow.02.02_105` | `N/A (Stateless Validation)` |
| **02.2.01** | `02201` | Teklif Listesi (200) | `02_201_quote_list_returns_200.ps1` | `VF-02201` | `ACT-02201` | `i18n.voltflow.02.02_201` | `N/A (Stateless Query)` |
| **02.2.02** | `02202` | Var Olmayan Teklif ID (404) | `02_202_quote_get_by_id_returns_404.ps1` | `VF-02202` | `ACT-02202` | `i18n.voltflow.02.02_202` | `N/A (Entity Not Found)` |
| **02.2.03** | `02203` | Teklif Taslağı Oluştur ve Kalem Ekle (200) | `02_203_create_quote_draft_and_add_items.ps1` | `VF-02203` | `ACT-02203` | `i18n.voltflow.02.02_203` | `Quote[None -> Draft]` |
| **02.3.01** | `02301` | Kalemsiz Teklif Yayınlama Reddi (400) | `02_301_empty_quote_issue_rejected_400.ps1` | `VF-02301` | `ACT-02301` | `i18n.voltflow.02.02_301` | `Quote[Draft -> Issued] (Guard: Items.Count > 0)` |
| **02.3.02** | `02302` | Geçersiz Teklif Kalemi Engeli (422) | `02_302_quote_item_invalid_price_or_quantity_rejected_422.ps1` | `VF-02302` | `ACT-02302` | `i18n.voltflow.02.02_302` | `QuoteItem[Validation Guard: Quantity > 0 && UnitPrice >= 0]` |
| **02.4.01** | `02401` | Teklif Kalem Toplamı, Kabul ve Peşinat | `02_401_quote_items_total_and_accept_with_deposit.ps1` | `VF-02401` | `ACT-02401` | `i18n.voltflow.02.02_401` | `Quote[Issued -> Accepted] (Guard: DepositPaid >= RequiredDeposit)` |
| **02.4.02** | `02402` | Teklif Reddi ve FSM Koruması (VF-02401) | `02_402_quote_rejection_with_reason.ps1` | `VF-02402` | `ACT-02402` | `i18n.voltflow.02.02_402` | `Quote[Issued -> Rejected] (Reason required)` |
| **02.4.03** | `02403` | Teklif Zaman Aşımı Koruması (VF-02401) | `02_403_quote_expire_transition.ps1` | `VF-02403` | `ACT-02403` | `i18n.voltflow.02.02_403` | `Quote[Issued -> Expired] (TimeWindow Guard)` |
| **02.4.04** | `02404` | Geçersiz Peşinat Ödemesi Reddi (422) | `02_404_invalid_deposit_payment_rejected_422.ps1` | `VF-02404` | `ACT-02404` | `i18n.voltflow.02.02_404` | `QuoteDeposit[Validation Guard: Deposit > Total]` |
| **02.4.05** | `02405` | Kabul Edilmiş Teklifi Tekrar Kabul Etme (422) | `02_405_accept_already_accepted_quote_rejected_422.ps1` | `VF-02405` | `ACT-02405` | `i18n.voltflow.02.02_405` | `Quote[FSM Conflict: Already Accepted -> Rejected]` |
| **02.4.06** | `02406` | Red Edilmiş Teklifi Kabul Etme (422) | `02_406_accept_rejected_quote_returns_422.ps1` | `VF-02406` | `ACT-02406` | `i18n.voltflow.02.02_406` | `Quote[FSM Conflict: Rejected -> Accept Forbidden]` |
| **02.4.07** | `02407` | Süresi Dolmuş Teklifi Kabul Etme (422) | `02_407_accept_expired_quote_returns_422.ps1` | `VF-02407` | `ACT-02407` | `i18n.voltflow.02.02_407` | `Quote[FSM Conflict: Expired -> Accept Forbidden]` |
| **02.5.01** | `02501` | Teklif -> İş Emri Dönüşümü ve Idempotency | `02_501_quote_to_work_order_idempotent_conversion.ps1` | `VF-02501` | `ACT-02501` | `i18n.voltflow.02.02_501` | `Quote[Accepted] -> WorkOrder[Open] (Idempotent Conversion)` |
| **02.5.02** | `02502` | Onaysız Tekliften İş Emri Üretim Engeli (422) | `02_502_unaccepted_quote_to_work_order_rejected_422.ps1` | `VF-02502` | `ACT-02502` | `i18n.voltflow.02.02_502` | `Quote[Draft/Issued] -> WorkOrder[Forbidden: Not Accepted]` |
| **02.6.01** | `02601` | Proje ve Faz Yönetimi (VF-02501) | `02_601_project_creation_and_phase_management.ps1` | `VF-02601` | `ACT-02601` | `i18n.voltflow.02.02_601` | `Project[Created] -> ProjectPhase[Scheduled]` |
| **02.6.02** | `02602` | Proje Listesi (200) | `02_602_project_list_returns_200.ps1` | `VF-02602` | `ACT-02602` | `i18n.voltflow.02.02_602` | `N/A (Stateless Query)` |
| **02.6.03** | `02603` | Var Olmayan Proje ID (404) | `02_603_project_get_by_id_returns_404.ps1` | `VF-02603` | `ACT-02603` | `i18n.voltflow.02.02_603` | `N/A (Entity Not Found)` |


### Modül [03]: Work Order Execution & FSM (21 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **03.1.01** | `03101` | İSG Kontrol Listesi Bariyeri | `03_101_safety_checklist_gate_enforcement.ps1` | `VF-03101` | `ACT-03101` | `i18n.voltflow.03.03_101` | `WorkOrder[Open] (Safety Checklist Incomplete -> Start Forbidden)` |
| **03.1.02** | `03102` | İş Emri Atama ve Yola Çıkış (VF-03101) | `03_102_work_order_assignment_and_enroute.ps1` | `VF-03102` | `ACT-03102` | `i18n.voltflow.03.03_102` | `WorkOrder[Open -> Assigned -> EnRoute]` |
| **03.1.03** | `03103` | Adreste Bulunamama (NoShow) Raporu (VF-03101) | `03_103_noshow_reporting_and_state_transition.ps1` | `VF-03103` | `ACT-03103` | `i18n.voltflow.03.03_103` | `WorkOrder[Assigned/EnRoute -> NoShow]` |
| **03.1.04** | `03104` | Atanmamış İş Emrini Yola Çıkarma Engeli | `03_104_enroute_without_assigned_rejected_422.ps1` | `VF-03104` | `ACT-03104` | `i18n.voltflow.03.03_104` | `WorkOrder[Open -> EnRoute Forbidden: Must Assign First]` |
| **03.1.05** | `03105` | İş Emri Listesi (200) | `03_105_work_order_list_returns_200.ps1` | `VF-03105` | `ACT-03105` | `i18n.voltflow.03.03_105` | `N/A (Stateless Query)` |
| **03.1.06** | `03106` | Var Olmayan İş Emri ID (404) | `03_106_work_order_get_by_id_returns_404.ps1` | `VF-03106` | `ACT-03106` | `i18n.voltflow.03.03_106` | `N/A (Stateless Operation)` |
| **03.1.07** | `03107` | İş Emri Oluşturma (200) | `03_107_create_work_order_success.ps1` | `VF-03107` | `ACT-03107` | `i18n.voltflow.03.03_107` | `N/A (Stateless Operation)` |
| **03.1.08** | `03108` | İş Emri Tam Durum Makinesi (Happy Path) | `03_108_full_state_machine_happy_path.ps1` | `VF-03108` | `ACT-03108` | `i18n.voltflow.03.03_108` | `N/A (Stateless Operation)` |
| **03.2.01** | `03201` | Zaman Takibi ve Çift Check-In Engeli | `03_201_time_tracking_and_double_checkin_prevention.ps1` | `VF-03201` | `ACT-03201` | `i18n.voltflow.03.03_201` | `WorkOrder[Assigned/EnRoute -> InProgress] (Guard: SafetyCompleted)` |
| **03.2.02** | `03202` | Açık Oturumsuz Check-Out Engeli (422) | `03_202_checkout_without_checkin_rejected_422.ps1` | `VF-03202` | `ACT-03202` | `i18n.voltflow.03.03_202` | `WorkOrderTimeEntry[Active -> Stopped] && DurationCalculated` |
| **03.3.01** | `03301` | Beklemeye Alma ve Otomatik Check-Out Telafisi | `03_301_hold_state_auto_checkout_compensation.ps1` | `VF-03301` | `ACT-03301` | `i18n.voltflow.03.03_301` | `WorkOrder[InProgress -> OnHold] (Guard: HoldReason != null)` |
| **03.3.02** | `03302` | Sahada Malzeme Tüketimi (VF-03401) | `03_302_field_material_addition_to_work_order.ps1` | `VF-03302` | `ACT-03302` | `i18n.voltflow.03.03_302` | `WorkOrder[OnHold -> InProgress]` |
| **03.3.03** | `03303` | Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422) | `03_303_add_material_to_completed_work_order_rejected_422.ps1` | `VF-03303` | `ACT-03303` | `i18n.voltflow.03.03_303` | `WorkOrder[Closed -> PutOnHold Forbidden]` |
| **03.4.01** | `03401` | Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama | `03_401_work_order_completion_with_proof_and_outbox.ps1` | `VF-03401` | `ACT-03401` | `i18n.voltflow.03.03_401` | `WorkOrder[InProgress -> Completed] (Guard: Signature != null && Photo != null)` |
| **03.4.02** | `03402` | Kanıtsız Tamamlama Engeli (422) | `03_402_complete_without_proof_rejected_422.ps1` | `VF-03402` | `ACT-03402` | `i18n.voltflow.03.03_402` | `WorkOrder[FSM Violation: Complete without InProgress -> Rejected]` |
| **03.5.01** | `03501` | Onay Bariyeri ve Terminal Faturalanmış Durum | `03_501_review_gate_and_terminal_invoiced_state.ps1` | `VF-03501` | `ACT-03501` | `i18n.voltflow.03.03_501` | `WorkOrder[Completed -> ReadyForBilling] (Gate Approved)` |
| **03.5.02** | `03502` | İş Emri İptali ve FSM Koruması (VF-03501) | `03_502_work_order_cancellation_and_terminal_guard.ps1` | `VF-03502` | `ACT-03502` | `i18n.voltflow.03.03_502` | `WorkOrder[ReadyForBilling -> Invoiced]` |
| **03.5.03** | `03503` | Beklemede Olmayan İş Emri Resume Engeli (422) | `03_503_resume_unheld_work_order_rejected_422.ps1` | `VF-03503` | `ACT-03503` | `i18n.voltflow.03.03_503` | `WorkOrder[!Completed && !Invoiced -> Cancelled]` |
| **03.5.04** | `03504` | Onaysız Faturalama Engeli (422) | `03_504_invoice_unapproved_work_order_rejected_422.ps1` | `VF-03504` | `ACT-03504` | `i18n.voltflow.03.03_504` | `WorkOrder[FSM Violation: Invoice without ReadyForBilling -> Rejected]` |
| **03.5.05** | `03505` | İptal Edilmiş İş Emrini Başlatma (422) | `03_505_start_cancelled_work_order_rejected_422.ps1` | `VF-03505` | `ACT-03505` | `i18n.voltflow.03.03_505` | `WorkOrder[Completed/Invoiced -> Cancel Forbidden]` |
| **03.5.06** | `03506` | Tamamlanan İş Emri Faturalama Onayı | `03_506_approve_billing_after_completion.ps1` | `VF-03506` | `ACT-03506` | `i18n.voltflow.03.03_506` | `N/A (Stateless Operation)` |


### Modül [04]: Inventory & Warehouse (9 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **04.1.01** | `04101` | Negatif Stok Bariyeri ve Atomik Geri Alma | `04_101_negative_stock_gate_and_atomic_rollback.ps1` | `VF-04101` | `ACT-04101` | `i18n.voltflow.04.04_101` | `StockLevel[WarehouseStockAdjusted]` |
| **04.1.02** | `04102` | Stok Sorgulama ve 404 Doğrulaması (VF-04101) | `04_102_get_stock_by_material_code_and_404.ps1` | `VF-04102` | `ACT-04102` | `i18n.voltflow.04.04_102` | `StockLevel[Guard: NegativeStockRejected]` |
| **04.1.03** | `04103` | Pozitif Stok Ayarlama (200) | `04_103_stock_adjust_positive_delta.ps1` | `VF-04103` | `ACT-04103` | `i18n.voltflow.04.04_103` | `N/A (Stateless Query)` |
| **04.1.04** | `04104` | Sıfır Delta ile Stok Ayarlama (422) | `04_104_stock_adjust_zero_delta_rejected_422.ps1` | `VF-04104` | `ACT-04104` | `i18n.voltflow.04.04_104` | `WarehouseTransfer[StockReserved -> StockMoved]` |
| **04.2.01** | `04201` | Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201) | `04_201_stock_reservation_and_available_reduction.ps1` | `VF-04201` | `ACT-04201` | `i18n.voltflow.04.04_201` | `StockReservation[Pending -> Reserved]` |
| **04.2.02** | `04202` | Yetersiz Stok Rezervasyonu Reddi | `04_202_insufficient_stock_reservation_rejected.ps1` | `VF-04202` | `ACT-04202` | `i18n.voltflow.04.04_202` | `StockReservation[Reserved -> Released]` |
| **04.2.03** | `04203` | Sıfır/Negatif Stok Rezervasyon Engeli (422) | `04_203_stock_reservation_zero_quantity_rejected_422.ps1` | `VF-04203` | `ACT-04203` | `i18n.voltflow.04.04_203` | `StockReservation[Guard: InsufficientStockRejected]` |
| **04.2.04** | `04204` | Negatif Stok Rezervasyonu Engeli (400/422) | `04_204_stock_reservation_negative_quantity_rejected_422.ps1` | `VF-04204` | `ACT-04204` | `i18n.voltflow.04.04_204` | `N/A (Validation Guard: Negatif Stok Rezervasyonu Engeli (400/422))` |
| **04.2.05** | `04205` | Ardışık Rezervasyonlar Müsait Stoğu Azaltır | `04_205_sequential_reservations_reduce_available.ps1` | `VF-04205` | `ACT-04205` | `i18n.voltflow.04.04_205` | `N/A (Stateless Operation)` |


### Modül [05]: Finance & Invoicing (14 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **05.1.01** | `05101` | Müşteri Fatura Listeleme ve Bakiye (VF-05101) | `05_101_list_invoices_by_customer.ps1` | `VF-05101` | `ACT-05101` | `i18n.voltflow.05.05_101` | `SalesInvoice[Draft -> Issued]` |
| **05.1.02** | `05102` | Proje Hakediş Girişi (VF-05101) | `05_102_billing_entry_creation_for_project.ps1` | `VF-05102` | `ACT-05102` | `i18n.voltflow.05.05_102` | `SalesInvoice[Guard: ZeroAmountRejected]` |
| **05.1.03** | `05103` | Müşteriye Göre Ödeme Listesi (200) | `05_103_list_payments_by_customer.ps1` | `VF-05103` | `ACT-05103` | `i18n.voltflow.05.05_103` | `N/A (Stateless Query)` |
| **05.1.04** | `05104` | Projeye Göre Billing Listesi (200) | `05_104_list_billing_entries_by_project.ps1` | `VF-05104` | `ACT-05104` | `i18n.voltflow.05.05_104` | `N/A (Stateless Query)` |
| **05.2.01** | `05201` | Tahsilat ve Cari Alacak Kaydı (VF-05201) | `05_201_customer_payment_and_ledger_credit.ps1` | `VF-05201` | `ACT-05201` | `i18n.voltflow.05.05_201` | `CustomerPayment[Pending -> Received]` |
| **05.2.02** | `05202` | Sıfır/Eksi Tahsilat Giriş Engeli (422) | `05_202_zero_or_negative_payment_amount_rejected_422.ps1` | `VF-05202` | `ACT-05202` | `i18n.voltflow.05.05_202` | `CustomerPayment[Guard: NegativePaymentRejected]` |
| **05.2.03** | `05203` | Geçersiz Müşteri ID ile Ödeme (422) | `05_203_payment_with_invalid_customer_rejected_422.ps1` | `VF-05203` | `ACT-05203` | `i18n.voltflow.05.05_203` | `N/A (Stateless Operation)` |
| **05.2.04** | `05204` | Geçersiz Ödeme Yöntemi (422) | `05_204_invalid_payment_method_rejected_422.ps1` | `VF-05204` | `ACT-05204` | `i18n.voltflow.05.05_204` | `N/A (Stateless Operation)` |
| **05.3.01** | `05301` | Tahsilat Fatura Mahsubu (VF-05301) | `05_301_payment_allocation_to_invoice.ps1` | `VF-05301` | `ACT-05301` | `i18n.voltflow.05.05_301` | `SalesInvoice[Issued -> PartiallyPaid/Paid] (Payment Allocated)` |
| **05.3.02** | `05302` | Aşırı Mahsup Engeli (VF-05302) | `05_302_over_allocation_rejected.ps1` | `VF-05302` | `ACT-05302` | `i18n.voltflow.05.05_302` | `PaymentAllocation[Guard: AllocationExceedsInvoiceBalance]` |
| **05.3.03** | `05303` | Fatura Bakiyesini Aşan Mahsup Engeli (422) | `05_303_allocation_exceeding_invoice_remaining_rejected_422.ps1` | `VF-05303` | `ACT-05303` | `i18n.voltflow.05.05_303` | `PaymentAllocation[Guard: AllocationExceedsPaymentAmount]` |
| **05.3.04** | `05304` | Var Olmayan Faturaya Mahsup (422) | `05_304_allocation_to_nonexistent_invoice_rejected.ps1` | `VF-05304` | `ACT-05304` | `i18n.voltflow.05.05_304` | `N/A (Stateless Operation)` |
| **05.3.05** | `05305` | Sıfır Tutarlı Mahsup (422) | `05_305_zero_amount_allocation_rejected_422.ps1` | `VF-05305` | `ACT-05305` | `i18n.voltflow.05.05_305` | `N/A (Stateless Operation)` |
| **05.4.01** | `05401` | Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401) | `05_401_optimistic_concurrency_duplicate_allocation_rejected.ps1` | `VF-05401` | `ACT-05401` | `i18n.voltflow.05.05_401` | `CustomerLedgerEntry[Posted]` |


### Modül [07]: System & Integrations (3 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **07.2.01** | `07201` | Dead-Letter Outbox Kuyruğu (VF-07201) | `07_201_operations_dead_letter_outbox_query.ps1` | `VF-07201` | `ACT-07201` | `i18n.voltflow.07.07_201` | `OutboxMessage[Pending -> Dispatched / Failed]` |
| **07.2.02** | `07202` | Yetkisiz Operasyon Erişimi Reddi (403) | `07_202_non_admin_operations_dead_letter_rejected_403.ps1` | `VF-07202` | `ACT-07202` | `i18n.voltflow.07.07_202` | `OutboxMessage[Failed -> Retried / PoisonQueue]` |
| **07.2.03** | `07203` | Var Olmayan Outbox Replay (404) | `07_203_outbox_replay_nonexistent_returns_404.ps1` | `VF-07203` | `ACT-07203` | `i18n.voltflow.07.07_203` | `N/A (Stateless Operation)` |


### Modül [08]: Security & Resilience (8 Senaryo)

| VUT Kodu | Düz Kod | Senaryo Adı & Doğrulama Hedefi | API Test Dosyası | RFC 7807 Hata | Log ACT | i18n Anahtarı (Halka 17) | FSM Geçiş Kuralı (Halka 18) |
| :---: | :---: | :--- | :--- | :---: | :---: | :--- | :--- |
| **08.1.01** | `08101` | Idempotency Key Uyuşmazlığı (409) | `08_101_idempotency_key_payload_mismatch_rejected_409.ps1` | `VF-08101` | `ACT-08101` | `i18n.voltflow.08.08_101` | `IdempotencyRecord[InFlight -> Completed / 409 Conflict]` |
| **08.2.01** | `08201` | Dağıtık Hız Sınırlama Bariyeri (429) | `08_201_mutation_rate_limit_exceeded_returns_429.ps1` | `VF-08201` | `ACT-08201` | `i18n.voltflow.08.08_201` | `RateLimitBucket[Incremented / Throttled 429]` |
| **08.4.01** | `08401` | RFC 7807 Problem Details Uyumluluğu | `08_401_rfc7807_problem_details_structure_compliance.ps1` | `VF-08401` | `ACT-08401` | `i18n.voltflow.08.08_401` | `N/A (RFC 7807 Error Response Formatting)` |
| **08.4.02** | `08402` | Geçersiz Content-Type (415) | `08_402_invalid_content_type_returns_415.ps1` | `VF-08402` | `ACT-08402` | `i18n.voltflow.08.08_402` | `N/A (Validation Exception Mapping 422)` |
| **08.4.03** | `08403` | Boş Body ile POST İsteği (400) | `08_403_empty_body_post_returns_400.ps1` | `VF-08403` | `ACT-08403` | `i18n.voltflow.08.08_403` | `N/A (Unauthorized Access Mapping 401)` |
| **08.4.04** | `08404` | Bozuk JSON ile POST İsteği (400) | `08_404_malformed_json_returns_400.ps1` | `VF-08404` | `ACT-08404` | `i18n.voltflow.08.08_404` | `N/A (Forbidden Access Mapping 403)` |
| **08.4.05** | `08405` | Var Olmayan Endpoint (404) | `08_405_nonexistent_endpoint_returns_404.ps1` | `VF-08405` | `ACT-08405` | `i18n.voltflow.08.08_405` | `N/A (Resource Not Found Mapping 404)` |
| **08.4.06** | `08406` | Yanlış HTTP Metodu (405) | `08_406_method_not_allowed_returns_405.ps1` | `VF-08406` | `ACT-08406` | `i18n.voltflow.08.08_406` | `N/A (Global Unhandled Exception 500)` |

---

## 🔍 6. CLI Üzerinden Nokta Atışı Arama Kullanımı

Terminalde `scripts/find-vut.ps1` aracını kullanarak herhangi bir kodu anında sorgulayabilirsiniz:

```powershell
# 1. Dosya kodundan arama:
pwsh .\scripts\find-vut.ps1 01_103

# 2. Düz VUT kodundan arama:
pwsh .\scripts\find-vut.ps1 02101

# 3. Hata kodundan arama:
pwsh .\scripts\find-vut.ps1 VF-01101

# 4. Aksiyon kimliğinden arama:
pwsh .\scripts\find-vut.ps1 ACT-03101

# 5. Kelimeden arama:
pwsh .\scripts\find-vut.ps1 "password"
```

---

## 📖 İlgili Belgeler
- 📘 [Voltflow 18-Halkalı Evrensel İzlenebilirlik Matrisi](voltflow-18-ring-traceability-matrix.md)
- 📗 [Voltflow Kapsamlı Dry-Test Rehberi](voltflow-dry-tests-guide.md)
- 📙 [Voltflow Tüm İş Akışları ve Hata Modları](voltflow-workflows-and-failure-modes.md)
- 📕 [Master VUT Taxonomy (Domain Sabitleri)](../../src/Voltflow.Domain/Common/VoltflowTaxonomy.cs)
