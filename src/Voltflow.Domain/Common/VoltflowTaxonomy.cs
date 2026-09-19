namespace Voltflow.Domain.Common;

/// <summary>
/// Voltflow Unified Taxonomy (VUT) - Master Canonical Dictionary (16-Ring Parity)
/// Structure: [Module].[Feature].[Scenario] -> MM.F.SS (or MMFSS)
/// </summary>
public static class VoltflowTaxonomy
{
    public static class ErrorCodes
    {
        // --- Module 01: Identity & Auth ---
        public const string VF_01101 = "VF-01101"; // Hatalı Şifre ile Giriş Reddi (401)
        public const string VF_01102 = "VF-01102"; // Onaysız Kullanıcı Giriş Reddi (403)
        public const string VF_01103 = "VF-01103"; // Eksik Email ile Login (400)
        public const string VF_01104 = "VF-01104"; // Şifresiz Kayıt Denemesi (400)
        public const string VF_01201 = "VF-01201"; // Master OTP (000000) ile Kayıt (200/202)
        public const string VF_01202 = "VF-01202"; // Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403
        public const string VF_01203 = "VF-01203"; // Mükerrer E-posta Kaydı Reddi (400/409)
        public const string VF_01204 = "VF-01204"; // Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK)
        public const string VF_01301 = "VF-01301"; // Oturum İptal Etme (204)
        public const string VF_01302 = "VF-01302"; // İptal Edilmiş Token ile Erişim (401)
        public const string VF_01303 = "VF-01303"; // Token Olmadan Korumalı Endpoint (401)
        public const string VF_01304 = "VF-01304"; // Geçersiz JWT Token (401)
        public const string VF_01401 = "VF-01401"; // Şifre Sıfırlama ve Yeni Şifre ile Giriş (200)
        public const string VF_01402 = "VF-01402"; // Geçersiz Şifre Sıfırlama Token'ı (400)
        public const string VF_01403 = "VF-01403"; // Yönetici Kullanıcı Onayı (VF-01101)
        public const string VF_01404 = "VF-01404"; // Yetkisiz Kullanıcı Onay Reddi (403)
        public const string VF_01405 = "VF-01405"; // Yönetici Tarafından Rol Ataması (VF-01101)
        public const string VF_01406 = "VF-01406"; // Tanımsız Rol Atama Reddi (422)
        public const string VF_01407 = "VF-01407"; // Var Olmayan Kullanıcıyı Onaylama (404)

        // --- Module 02: Quote to Order & CRM ---
        public const string VF_02101 = "VF-02101"; // Müşteri Lead Başlatma ve Aktivasyon
        public const string VF_02102 = "VF-02102"; // Mükerrer Müşteri Kayıt Reddi (422)
        public const string VF_02103 = "VF-02103"; // Müşteri Detay ve 404 Koruması (VF-02101)
        public const string VF_02104 = "VF-02104"; // Müşteri Listesi (200)
        public const string VF_02105 = "VF-02105"; // Eksik Email ile Müşteri Oluşturma (422)
        public const string VF_02201 = "VF-02201"; // Teklif Listesi (200)
        public const string VF_02202 = "VF-02202"; // Var Olmayan Teklif ID (404)
        public const string VF_02203 = "VF-02203"; // Teklif Taslağı Oluştur ve Kalem Ekle (200)
        public const string VF_02301 = "VF-02301"; // Kalemsiz Teklif Yayınlama Reddi (400)
        public const string VF_02302 = "VF-02302"; // Geçersiz Teklif Kalemi Engeli (422)
        public const string VF_02401 = "VF-02401"; // Teklif Kalem Toplamı, Kabul ve Peşinat
        public const string VF_02402 = "VF-02402"; // Teklif Reddi ve FSM Koruması (VF-02401)
        public const string VF_02403 = "VF-02403"; // Teklif Zaman Aşımı Koruması (VF-02401)
        public const string VF_02404 = "VF-02404"; // Geçersiz Peşinat Ödemesi Reddi (422)
        public const string VF_02405 = "VF-02405"; // Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)
        public const string VF_02406 = "VF-02406"; // Red Edilmiş Teklifi Kabul Etme (422)
        public const string VF_02407 = "VF-02407"; // Süresi Dolmuş Teklifi Kabul Etme (422)
        public const string VF_02501 = "VF-02501"; // Teklif -> İş Emri Dönüşümü ve Idempotency
        public const string VF_02502 = "VF-02502"; // Onaysız Tekliften İş Emri Üretim Engeli (422)
        public const string VF_02601 = "VF-02601"; // Proje ve Faz Yönetimi (VF-02501)
        public const string VF_02602 = "VF-02602"; // Proje Listesi (200)
        public const string VF_02603 = "VF-02603"; // Var Olmayan Proje ID (404)

        // --- Module 03: Work Order Execution & FSM ---
        public const string VF_03101 = "VF-03101"; // İSG Kontrol Listesi Bariyeri
        public const string VF_03102 = "VF-03102"; // İş Emri Atama ve Yola Çıkış (VF-03101)
        public const string VF_03103 = "VF-03103"; // Adreste Bulunamama (NoShow) Raporu (VF-03101)
        public const string VF_03104 = "VF-03104"; // Atanmamış İş Emrini Yola Çıkarma Engeli
        public const string VF_03105 = "VF-03105"; // İş Emri Listesi (200)
        public const string VF_03106 = "VF-03106"; // Var Olmayan İş Emri ID (404)
        public const string VF_03107 = "VF-03107"; // İş Emri Oluşturma (200)
        public const string VF_03108 = "VF-03108"; // İş Emri Tam Durum Makinesi (Happy Path)
        public const string VF_03201 = "VF-03201"; // Zaman Takibi ve Çift Check-In Engeli
        public const string VF_03202 = "VF-03202"; // Açık Oturumsuz Check-Out Engeli (422)
        public const string VF_03301 = "VF-03301"; // Beklemeye Alma ve Otomatik Check-Out Telafisi
        public const string VF_03302 = "VF-03302"; // Sahada Malzeme Tüketimi (VF-03401)
        public const string VF_03303 = "VF-03303"; // Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422)
        public const string VF_03401 = "VF-03401"; // Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama
        public const string VF_03402 = "VF-03402"; // Kanıtsız Tamamlama Engeli (422)
        public const string VF_03501 = "VF-03501"; // Onay Bariyeri ve Terminal Faturalanmış Durum
        public const string VF_03502 = "VF-03502"; // İş Emri İptali ve FSM Koruması (VF-03501)
        public const string VF_03503 = "VF-03503"; // Beklemede Olmayan İş Emri Resume Engeli (422)
        public const string VF_03504 = "VF-03504"; // Onaysız Faturalama Engeli (422)
        public const string VF_03505 = "VF-03505"; // İptal Edilmiş İş Emrini Başlatma (422)
        public const string VF_03506 = "VF-03506"; // Tamamlanan İş Emri Faturalama Onayı

        // --- Module 04: Inventory & Warehouse ---
        public const string VF_04101 = "VF-04101"; // Negatif Stok Bariyeri ve Atomik Geri Alma
        public const string VF_04102 = "VF-04102"; // Stok Sorgulama ve 404 Doğrulaması (VF-04101)
        public const string VF_04103 = "VF-04103"; // Pozitif Stok Ayarlama (200)
        public const string VF_04104 = "VF-04104"; // Sıfır Delta ile Stok Ayarlama (422)
        public const string VF_04201 = "VF-04201"; // Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201)
        public const string VF_04202 = "VF-04202"; // Yetersiz Stok Rezervasyonu Reddi
        public const string VF_04203 = "VF-04203"; // Sıfır/Negatif Stok Rezervasyon Engeli (422)
        public const string VF_04204 = "VF-04204"; // Negatif Stok Rezervasyonu Engeli (400/422)
        public const string VF_04205 = "VF-04205"; // Ardışık Rezervasyonlar Müsait Stoğu Azaltır

        // --- Module 05: Finance & Invoicing ---
        public const string VF_05101 = "VF-05101"; // Müşteri Fatura Listeleme ve Bakiye (VF-05101)
        public const string VF_05102 = "VF-05102"; // Proje Hakediş Girişi (VF-05101)
        public const string VF_05103 = "VF-05103"; // Müşteriye Göre Ödeme Listesi (200)
        public const string VF_05104 = "VF-05104"; // Projeye Göre Billing Listesi (200)
        public const string VF_05201 = "VF-05201"; // Tahsilat ve Cari Alacak Kaydı (VF-05201)
        public const string VF_05202 = "VF-05202"; // Sıfır/Eksi Tahsilat Giriş Engeli (422)
        public const string VF_05203 = "VF-05203"; // Geçersiz Müşteri ID ile Ödeme (422)
        public const string VF_05204 = "VF-05204"; // Geçersiz Ödeme Yöntemi (422)
        public const string VF_05301 = "VF-05301"; // Tahsilat Fatura Mahsubu (VF-05301)
        public const string VF_05302 = "VF-05302"; // Aşırı Mahsup Engeli (VF-05302)
        public const string VF_05303 = "VF-05303"; // Fatura Bakiyesini Aşan Mahsup Engeli (422)
        public const string VF_05304 = "VF-05304"; // Var Olmayan Faturaya Mahsup (422)
        public const string VF_05305 = "VF-05305"; // Sıfır Tutarlı Mahsup (422)
        public const string VF_05401 = "VF-05401"; // Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401)

        // --- Module 07: Operations & Outbox ---
        public const string VF_07201 = "VF-07201"; // Dead-Letter Outbox Kuyruğu (VF-07201)
        public const string VF_07202 = "VF-07202"; // Yetkisiz Operasyon Erişimi Reddi (403)
        public const string VF_07203 = "VF-07203"; // Var Olmayan Outbox Replay (404)

        // --- Module 08: Security & Resilience ---
        public const string VF_08101 = "VF-08101"; // Idempotency Key Uyuşmazlığı (409)
        public const string VF_08201 = "VF-08201"; // Dağıtık Hız Sınırlama Bariyeri (429)
        public const string VF_08401 = "VF-08401"; // RFC 7807 Problem Details Uyumluluğu
        public const string VF_08402 = "VF-08402"; // Geçersiz Content-Type (415)
        public const string VF_08403 = "VF-08403"; // Boş Body ile POST İsteği (400)
        public const string VF_08404 = "VF-08404"; // Bozuk JSON ile POST İsteği (400)
        public const string VF_08405 = "VF-08405"; // Var Olmayan Endpoint (404)
        public const string VF_08406 = "VF-08406"; // Yanlış HTTP Metodu (405)

        public static string Get(string vutFlat) => $"VF-{vutFlat.Replace("_", "").Replace(".", "")}";
    }

    public static class Actions
    {
        // --- Module 01: Identity & Auth ---
        public const string ACT_01101 = "ACT-01101"; // Hatalı Şifre ile Giriş Reddi (401)
        public const string ACT_01102 = "ACT-01102"; // Onaysız Kullanıcı Giriş Reddi (403)
        public const string ACT_01103 = "ACT-01103"; // Eksik Email ile Login (400)
        public const string ACT_01104 = "ACT-01104"; // Şifresiz Kayıt Denemesi (400)
        public const string ACT_01201 = "ACT-01201"; // Master OTP (000000) ile Kayıt (200/202)
        public const string ACT_01202 = "ACT-01202"; // Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403
        public const string ACT_01203 = "ACT-01203"; // Mükerrer E-posta Kaydı Reddi (400/409)
        public const string ACT_01204 = "ACT-01204"; // Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK)
        public const string ACT_01301 = "ACT-01301"; // Oturum İptal Etme (204)
        public const string ACT_01302 = "ACT-01302"; // İptal Edilmiş Token ile Erişim (401)
        public const string ACT_01303 = "ACT-01303"; // Token Olmadan Korumalı Endpoint (401)
        public const string ACT_01304 = "ACT-01304"; // Geçersiz JWT Token (401)
        public const string ACT_01401 = "ACT-01401"; // Şifre Sıfırlama ve Yeni Şifre ile Giriş (200)
        public const string ACT_01402 = "ACT-01402"; // Geçersiz Şifre Sıfırlama Token'ı (400)
        public const string ACT_01403 = "ACT-01403"; // Yönetici Kullanıcı Onayı (VF-01101)
        public const string ACT_01404 = "ACT-01404"; // Yetkisiz Kullanıcı Onay Reddi (403)
        public const string ACT_01405 = "ACT-01405"; // Yönetici Tarafından Rol Ataması (VF-01101)
        public const string ACT_01406 = "ACT-01406"; // Tanımsız Rol Atama Reddi (422)
        public const string ACT_01407 = "ACT-01407"; // Var Olmayan Kullanıcıyı Onaylama (404)

        // --- Module 02: Quote to Order & CRM ---
        public const string ACT_02101 = "ACT-02101"; // Müşteri Lead Başlatma ve Aktivasyon
        public const string ACT_02102 = "ACT-02102"; // Mükerrer Müşteri Kayıt Reddi (422)
        public const string ACT_02103 = "ACT-02103"; // Müşteri Detay ve 404 Koruması (VF-02101)
        public const string ACT_02104 = "ACT-02104"; // Müşteri Listesi (200)
        public const string ACT_02105 = "ACT-02105"; // Eksik Email ile Müşteri Oluşturma (422)
        public const string ACT_02201 = "ACT-02201"; // Teklif Listesi (200)
        public const string ACT_02202 = "ACT-02202"; // Var Olmayan Teklif ID (404)
        public const string ACT_02203 = "ACT-02203"; // Teklif Taslağı Oluştur ve Kalem Ekle (200)
        public const string ACT_02301 = "ACT-02301"; // Kalemsiz Teklif Yayınlama Reddi (400)
        public const string ACT_02302 = "ACT-02302"; // Geçersiz Teklif Kalemi Engeli (422)
        public const string ACT_02401 = "ACT-02401"; // Teklif Kalem Toplamı, Kabul ve Peşinat
        public const string ACT_02402 = "ACT-02402"; // Teklif Reddi ve FSM Koruması (VF-02401)
        public const string ACT_02403 = "ACT-02403"; // Teklif Zaman Aşımı Koruması (VF-02401)
        public const string ACT_02404 = "ACT-02404"; // Geçersiz Peşinat Ödemesi Reddi (422)
        public const string ACT_02405 = "ACT-02405"; // Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)
        public const string ACT_02406 = "ACT-02406"; // Red Edilmiş Teklifi Kabul Etme (422)
        public const string ACT_02407 = "ACT-02407"; // Süresi Dolmuş Teklifi Kabul Etme (422)
        public const string ACT_02501 = "ACT-02501"; // Teklif -> İş Emri Dönüşümü ve Idempotency
        public const string ACT_02502 = "ACT-02502"; // Onaysız Tekliften İş Emri Üretim Engeli (422)
        public const string ACT_02601 = "ACT-02601"; // Proje ve Faz Yönetimi (VF-02501)
        public const string ACT_02602 = "ACT-02602"; // Proje Listesi (200)
        public const string ACT_02603 = "ACT-02603"; // Var Olmayan Proje ID (404)

        // --- Module 03: Work Order Execution & FSM ---
        public const string ACT_03101 = "ACT-03101"; // İSG Kontrol Listesi Bariyeri
        public const string ACT_03102 = "ACT-03102"; // İş Emri Atama ve Yola Çıkış (VF-03101)
        public const string ACT_03103 = "ACT-03103"; // Adreste Bulunamama (NoShow) Raporu (VF-03101)
        public const string ACT_03104 = "ACT-03104"; // Atanmamış İş Emrini Yola Çıkarma Engeli
        public const string ACT_03105 = "ACT-03105"; // İş Emri Listesi (200)
        public const string ACT_03106 = "ACT-03106"; // Var Olmayan İş Emri ID (404)
        public const string ACT_03107 = "ACT-03107"; // İş Emri Oluşturma (200)
        public const string ACT_03108 = "ACT-03108"; // İş Emri Tam Durum Makinesi (Happy Path)
        public const string ACT_03201 = "ACT-03201"; // Zaman Takibi ve Çift Check-In Engeli
        public const string ACT_03202 = "ACT-03202"; // Açık Oturumsuz Check-Out Engeli (422)
        public const string ACT_03301 = "ACT-03301"; // Beklemeye Alma ve Otomatik Check-Out Telafisi
        public const string ACT_03302 = "ACT-03302"; // Sahada Malzeme Tüketimi (VF-03401)
        public const string ACT_03303 = "ACT-03303"; // Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422)
        public const string ACT_03401 = "ACT-03401"; // Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama
        public const string ACT_03402 = "ACT-03402"; // Kanıtsız Tamamlama Engeli (422)
        public const string ACT_03501 = "ACT-03501"; // Onay Bariyeri ve Terminal Faturalanmış Durum
        public const string ACT_03502 = "ACT-03502"; // İş Emri İptali ve FSM Koruması (VF-03501)
        public const string ACT_03503 = "ACT-03503"; // Beklemede Olmayan İş Emri Resume Engeli (422)
        public const string ACT_03504 = "ACT-03504"; // Onaysız Faturalama Engeli (422)
        public const string ACT_03505 = "ACT-03505"; // İptal Edilmiş İş Emrini Başlatma (422)
        public const string ACT_03506 = "ACT-03506"; // Tamamlanan İş Emri Faturalama Onayı

        // --- Module 04: Inventory & Warehouse ---
        public const string ACT_04101 = "ACT-04101"; // Negatif Stok Bariyeri ve Atomik Geri Alma
        public const string ACT_04102 = "ACT-04102"; // Stok Sorgulama ve 404 Doğrulaması (VF-04101)
        public const string ACT_04103 = "ACT-04103"; // Pozitif Stok Ayarlama (200)
        public const string ACT_04104 = "ACT-04104"; // Sıfır Delta ile Stok Ayarlama (422)
        public const string ACT_04201 = "ACT-04201"; // Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201)
        public const string ACT_04202 = "ACT-04202"; // Yetersiz Stok Rezervasyonu Reddi
        public const string ACT_04203 = "ACT-04203"; // Sıfır/Negatif Stok Rezervasyon Engeli (422)
        public const string ACT_04204 = "ACT-04204"; // Negatif Stok Rezervasyonu Engeli (400/422)
        public const string ACT_04205 = "ACT-04205"; // Ardışık Rezervasyonlar Müsait Stoğu Azaltır

        // --- Module 05: Finance & Invoicing ---
        public const string ACT_05101 = "ACT-05101"; // Müşteri Fatura Listeleme ve Bakiye (VF-05101)
        public const string ACT_05102 = "ACT-05102"; // Proje Hakediş Girişi (VF-05101)
        public const string ACT_05103 = "ACT-05103"; // Müşteriye Göre Ödeme Listesi (200)
        public const string ACT_05104 = "ACT-05104"; // Projeye Göre Billing Listesi (200)
        public const string ACT_05201 = "ACT-05201"; // Tahsilat ve Cari Alacak Kaydı (VF-05201)
        public const string ACT_05202 = "ACT-05202"; // Sıfır/Eksi Tahsilat Giriş Engeli (422)
        public const string ACT_05203 = "ACT-05203"; // Geçersiz Müşteri ID ile Ödeme (422)
        public const string ACT_05204 = "ACT-05204"; // Geçersiz Ödeme Yöntemi (422)
        public const string ACT_05301 = "ACT-05301"; // Tahsilat Fatura Mahsubu (VF-05301)
        public const string ACT_05302 = "ACT-05302"; // Aşırı Mahsup Engeli (VF-05302)
        public const string ACT_05303 = "ACT-05303"; // Fatura Bakiyesini Aşan Mahsup Engeli (422)
        public const string ACT_05304 = "ACT-05304"; // Var Olmayan Faturaya Mahsup (422)
        public const string ACT_05305 = "ACT-05305"; // Sıfır Tutarlı Mahsup (422)
        public const string ACT_05401 = "ACT-05401"; // Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401)

        // --- Module 07: Operations & Outbox ---
        public const string ACT_07201 = "ACT-07201"; // Dead-Letter Outbox Kuyruğu (VF-07201)
        public const string ACT_07202 = "ACT-07202"; // Yetkisiz Operasyon Erişimi Reddi (403)
        public const string ACT_07203 = "ACT-07203"; // Var Olmayan Outbox Replay (404)

        // --- Module 08: Security & Resilience ---
        public const string ACT_08101 = "ACT-08101"; // Idempotency Key Uyuşmazlığı (409)
        public const string ACT_08201 = "ACT-08201"; // Dağıtık Hız Sınırlama Bariyeri (429)
        public const string ACT_08401 = "ACT-08401"; // RFC 7807 Problem Details Uyumluluğu
        public const string ACT_08402 = "ACT-08402"; // Geçersiz Content-Type (415)
        public const string ACT_08403 = "ACT-08403"; // Boş Body ile POST İsteği (400)
        public const string ACT_08404 = "ACT-08404"; // Bozuk JSON ile POST İsteği (400)
        public const string ACT_08405 = "ACT-08405"; // Var Olmayan Endpoint (404)
        public const string ACT_08406 = "ACT-08406"; // Yanlış HTTP Metodu (405)

    }

    public static class Permissions
    {
        // --- Module 01: Identity & Auth ---
        public const string PERM_01101 = "PERM_01101"; // Hatalı Şifre ile Giriş Reddi (401)
        public const string PERM_01102 = "PERM_01102"; // Onaysız Kullanıcı Giriş Reddi (403)
        public const string PERM_01103 = "PERM_01103"; // Eksik Email ile Login (400)
        public const string PERM_01104 = "PERM_01104"; // Şifresiz Kayıt Denemesi (400)
        public const string PERM_01201 = "PERM_01201"; // Master OTP (000000) ile Kayıt (200/202)
        public const string PERM_01202 = "PERM_01202"; // Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403
        public const string PERM_01203 = "PERM_01203"; // Mükerrer E-posta Kaydı Reddi (400/409)
        public const string PERM_01204 = "PERM_01204"; // Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK)
        public const string PERM_01301 = "PERM_01301"; // Oturum İptal Etme (204)
        public const string PERM_01302 = "PERM_01302"; // İptal Edilmiş Token ile Erişim (401)
        public const string PERM_01303 = "PERM_01303"; // Token Olmadan Korumalı Endpoint (401)
        public const string PERM_01304 = "PERM_01304"; // Geçersiz JWT Token (401)
        public const string PERM_01401 = "PERM_01401"; // Şifre Sıfırlama ve Yeni Şifre ile Giriş (200)
        public const string PERM_01402 = "PERM_01402"; // Geçersiz Şifre Sıfırlama Token'ı (400)
        public const string PERM_01403 = "PERM_01403"; // Yönetici Kullanıcı Onayı (VF-01101)
        public const string PERM_01404 = "PERM_01404"; // Yetkisiz Kullanıcı Onay Reddi (403)
        public const string PERM_01405 = "PERM_01405"; // Yönetici Tarafından Rol Ataması (VF-01101)
        public const string PERM_01406 = "PERM_01406"; // Tanımsız Rol Atama Reddi (422)
        public const string PERM_01407 = "PERM_01407"; // Var Olmayan Kullanıcıyı Onaylama (404)

        // --- Module 02: Quote to Order & CRM ---
        public const string PERM_02101 = "PERM_02101"; // Müşteri Lead Başlatma ve Aktivasyon
        public const string PERM_02102 = "PERM_02102"; // Mükerrer Müşteri Kayıt Reddi (422)
        public const string PERM_02103 = "PERM_02103"; // Müşteri Detay ve 404 Koruması (VF-02101)
        public const string PERM_02104 = "PERM_02104"; // Müşteri Listesi (200)
        public const string PERM_02105 = "PERM_02105"; // Eksik Email ile Müşteri Oluşturma (422)
        public const string PERM_02201 = "PERM_02201"; // Teklif Listesi (200)
        public const string PERM_02202 = "PERM_02202"; // Var Olmayan Teklif ID (404)
        public const string PERM_02203 = "PERM_02203"; // Teklif Taslağı Oluştur ve Kalem Ekle (200)
        public const string PERM_02301 = "PERM_02301"; // Kalemsiz Teklif Yayınlama Reddi (400)
        public const string PERM_02302 = "PERM_02302"; // Geçersiz Teklif Kalemi Engeli (422)
        public const string PERM_02401 = "PERM_02401"; // Teklif Kalem Toplamı, Kabul ve Peşinat
        public const string PERM_02402 = "PERM_02402"; // Teklif Reddi ve FSM Koruması (VF-02401)
        public const string PERM_02403 = "PERM_02403"; // Teklif Zaman Aşımı Koruması (VF-02401)
        public const string PERM_02404 = "PERM_02404"; // Geçersiz Peşinat Ödemesi Reddi (422)
        public const string PERM_02405 = "PERM_02405"; // Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)
        public const string PERM_02406 = "PERM_02406"; // Red Edilmiş Teklifi Kabul Etme (422)
        public const string PERM_02407 = "PERM_02407"; // Süresi Dolmuş Teklifi Kabul Etme (422)
        public const string PERM_02501 = "PERM_02501"; // Teklif -> İş Emri Dönüşümü ve Idempotency
        public const string PERM_02502 = "PERM_02502"; // Onaysız Tekliften İş Emri Üretim Engeli (422)
        public const string PERM_02601 = "PERM_02601"; // Proje ve Faz Yönetimi (VF-02501)
        public const string PERM_02602 = "PERM_02602"; // Proje Listesi (200)
        public const string PERM_02603 = "PERM_02603"; // Var Olmayan Proje ID (404)

        // --- Module 03: Work Order Execution & FSM ---
        public const string PERM_03101 = "PERM_03101"; // İSG Kontrol Listesi Bariyeri
        public const string PERM_03102 = "PERM_03102"; // İş Emri Atama ve Yola Çıkış (VF-03101)
        public const string PERM_03103 = "PERM_03103"; // Adreste Bulunamama (NoShow) Raporu (VF-03101)
        public const string PERM_03104 = "PERM_03104"; // Atanmamış İş Emrini Yola Çıkarma Engeli
        public const string PERM_03105 = "PERM_03105"; // İş Emri Listesi (200)
        public const string PERM_03106 = "PERM_03106"; // Var Olmayan İş Emri ID (404)
        public const string PERM_03107 = "PERM_03107"; // İş Emri Oluşturma (200)
        public const string PERM_03108 = "PERM_03108"; // İş Emri Tam Durum Makinesi (Happy Path)
        public const string PERM_03201 = "PERM_03201"; // Zaman Takibi ve Çift Check-In Engeli
        public const string PERM_03202 = "PERM_03202"; // Açık Oturumsuz Check-Out Engeli (422)
        public const string PERM_03301 = "PERM_03301"; // Beklemeye Alma ve Otomatik Check-Out Telafisi
        public const string PERM_03302 = "PERM_03302"; // Sahada Malzeme Tüketimi (VF-03401)
        public const string PERM_03303 = "PERM_03303"; // Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422)
        public const string PERM_03401 = "PERM_03401"; // Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama
        public const string PERM_03402 = "PERM_03402"; // Kanıtsız Tamamlama Engeli (422)
        public const string PERM_03501 = "PERM_03501"; // Onay Bariyeri ve Terminal Faturalanmış Durum
        public const string PERM_03502 = "PERM_03502"; // İş Emri İptali ve FSM Koruması (VF-03501)
        public const string PERM_03503 = "PERM_03503"; // Beklemede Olmayan İş Emri Resume Engeli (422)
        public const string PERM_03504 = "PERM_03504"; // Onaysız Faturalama Engeli (422)
        public const string PERM_03505 = "PERM_03505"; // İptal Edilmiş İş Emrini Başlatma (422)
        public const string PERM_03506 = "PERM_03506"; // Tamamlanan İş Emri Faturalama Onayı

        // --- Module 04: Inventory & Warehouse ---
        public const string PERM_04101 = "PERM_04101"; // Negatif Stok Bariyeri ve Atomik Geri Alma
        public const string PERM_04102 = "PERM_04102"; // Stok Sorgulama ve 404 Doğrulaması (VF-04101)
        public const string PERM_04103 = "PERM_04103"; // Pozitif Stok Ayarlama (200)
        public const string PERM_04104 = "PERM_04104"; // Sıfır Delta ile Stok Ayarlama (422)
        public const string PERM_04201 = "PERM_04201"; // Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201)
        public const string PERM_04202 = "PERM_04202"; // Yetersiz Stok Rezervasyonu Reddi
        public const string PERM_04203 = "PERM_04203"; // Sıfır/Negatif Stok Rezervasyon Engeli (422)
        public const string PERM_04204 = "PERM_04204"; // Negatif Stok Rezervasyonu Engeli (400/422)
        public const string PERM_04205 = "PERM_04205"; // Ardışık Rezervasyonlar Müsait Stoğu Azaltır

        // --- Module 05: Finance & Invoicing ---
        public const string PERM_05101 = "PERM_05101"; // Müşteri Fatura Listeleme ve Bakiye (VF-05101)
        public const string PERM_05102 = "PERM_05102"; // Proje Hakediş Girişi (VF-05101)
        public const string PERM_05103 = "PERM_05103"; // Müşteriye Göre Ödeme Listesi (200)
        public const string PERM_05104 = "PERM_05104"; // Projeye Göre Billing Listesi (200)
        public const string PERM_05201 = "PERM_05201"; // Tahsilat ve Cari Alacak Kaydı (VF-05201)
        public const string PERM_05202 = "PERM_05202"; // Sıfır/Eksi Tahsilat Giriş Engeli (422)
        public const string PERM_05203 = "PERM_05203"; // Geçersiz Müşteri ID ile Ödeme (422)
        public const string PERM_05204 = "PERM_05204"; // Geçersiz Ödeme Yöntemi (422)
        public const string PERM_05301 = "PERM_05301"; // Tahsilat Fatura Mahsubu (VF-05301)
        public const string PERM_05302 = "PERM_05302"; // Aşırı Mahsup Engeli (VF-05302)
        public const string PERM_05303 = "PERM_05303"; // Fatura Bakiyesini Aşan Mahsup Engeli (422)
        public const string PERM_05304 = "PERM_05304"; // Var Olmayan Faturaya Mahsup (422)
        public const string PERM_05305 = "PERM_05305"; // Sıfır Tutarlı Mahsup (422)
        public const string PERM_05401 = "PERM_05401"; // Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401)

        // --- Module 07: Operations & Outbox ---
        public const string PERM_07201 = "PERM_07201"; // Dead-Letter Outbox Kuyruğu (VF-07201)
        public const string PERM_07202 = "PERM_07202"; // Yetkisiz Operasyon Erişimi Reddi (403)
        public const string PERM_07203 = "PERM_07203"; // Var Olmayan Outbox Replay (404)

        // --- Module 08: Security & Resilience ---
        public const string PERM_08101 = "PERM_08101"; // Idempotency Key Uyuşmazlığı (409)
        public const string PERM_08201 = "PERM_08201"; // Dağıtık Hız Sınırlama Bariyeri (429)
        public const string PERM_08401 = "PERM_08401"; // RFC 7807 Problem Details Uyumluluğu
        public const string PERM_08402 = "PERM_08402"; // Geçersiz Content-Type (415)
        public const string PERM_08403 = "PERM_08403"; // Boş Body ile POST İsteği (400)
        public const string PERM_08404 = "PERM_08404"; // Bozuk JSON ile POST İsteği (400)
        public const string PERM_08405 = "PERM_08405"; // Var Olmayan Endpoint (404)
        public const string PERM_08406 = "PERM_08406"; // Yanlış HTTP Metodu (405)

    }

    public static class IdempotencyScopes
    {
        public const string SCOPE_01201_Register = "SCOPE_01201_Register";
        public const string SCOPE_01301_RevokeSession = "SCOPE_01301_RevokeSession";
        public const string SCOPE_02101_CustomerCreate = "SCOPE_02101_CustomerCreate";
        public const string SCOPE_02102_CustomerConvertToActive = "SCOPE_02102_CustomerConvertToActive";
        public const string SCOPE_02301_QuoteIssue = "SCOPE_02301_QuoteIssue";
        public const string SCOPE_02401_QuoteAccept = "SCOPE_02401_QuoteAccept";
        public const string SCOPE_02501_QuoteToWorkOrder = "SCOPE_02501_QuoteToWorkOrder";
        public const string SCOPE_03101_ChecklistSave = "SCOPE_03101_ChecklistSave";
        public const string SCOPE_03201_CheckIn = "SCOPE_03201_CheckIn";
        public const string SCOPE_03202_CheckOut = "SCOPE_03202_CheckOut";
        public const string SCOPE_03401_WorkOrderComplete = "SCOPE_03401_WorkOrderComplete";
        public const string SCOPE_04101_StockAdjust = "SCOPE_04101_StockAdjust";
        public const string SCOPE_04201_StockReserve = "SCOPE_04201_StockReserve";
        public const string SCOPE_05201_PaymentCreate = "SCOPE_05201_PaymentCreate";
        public const string SCOPE_05301_PaymentAllocate = "SCOPE_05301_PaymentAllocate";
        public const string SCOPE_07201_OutboxReplay = "SCOPE_07201_OutboxReplay";
        public const string SCOPE_08101_IdempotencyGuard = "SCOPE_08101_IdempotencyGuard";
    }

    public static class EventTypes
    {
        public const string EVT_01101_UserApproved = "EVT_01101_UserApproved";
        public const string EVT_01201_UserRegistered = "EVT_01201_UserRegistered";
        public const string EVT_01301_SessionRevoked = "EVT_01301_SessionRevoked";
        public const string EVT_01401_PasswordResetRequested = "EVT_01401_PasswordResetRequested";
        public const string EVT_02101_CustomerActivated = "EVT_02101_CustomerActivated";
        public const string EVT_02301_QuoteIssued = "EVT_02301_QuoteIssued";
        public const string EVT_02401_QuoteAccepted = "EVT_02401_QuoteAccepted";
        public const string EVT_02501_WorkOrderCreatedFromQuote = "EVT_02501_WorkOrderCreatedFromQuote";
        public const string EVT_03101_SafetyChecklistCompleted = "EVT_03101_SafetyChecklistCompleted";
        public const string EVT_03201_TechnicianCheckedIn = "EVT_03201_TechnicianCheckedIn";
        public const string EVT_03202_TechnicianCheckedOut = "EVT_03202_TechnicianCheckedOut";
        public const string EVT_03301_WorkOrderPutOnHold = "EVT_03301_WorkOrderPutOnHold";
        public const string EVT_03401_WorkOrderCompleted = "EVT_03401_WorkOrderCompleted";
        public const string EVT_03501_WorkOrderApprovedForBilling = "EVT_03501_WorkOrderApprovedForBilling";
        public const string EVT_03502_WorkOrderInvoiced = "EVT_03502_WorkOrderInvoiced";
        public const string EVT_04101_StockAdjusted = "EVT_04101_StockAdjusted";
        public const string EVT_04201_StockReserved = "EVT_04201_StockReserved";
        public const string EVT_05101_SalesInvoiceGenerated = "EVT_05101_SalesInvoiceGenerated";
        public const string EVT_05201_CustomerPaymentReceived = "EVT_05201_CustomerPaymentReceived";
        public const string EVT_05301_PaymentAllocatedToInvoice = "EVT_05301_PaymentAllocatedToInvoice";
        public const string EVT_07201_OutboxDispatched = "EVT_07201_OutboxDispatched";
    }

    public static class Screens
    {
        public const string SCR_0110_Login = "SCR-0110";
        public const string SCR_0120_Register = "SCR-0120";
        public const string SCR_0130_Session = "SCR-0130";
        public const string SCR_0140_PasswordReset = "SCR-0140";
        public const string SCR_0210_Customers = "SCR-0210";
        public const string SCR_0220_CustomerSites = "SCR-0220";
        public const string SCR_0230_Quotes = "SCR-0230";
        public const string SCR_0240_QuoteDetail = "SCR-0240";
        public const string SCR_0250_QuoteConversion = "SCR-0250";
        public const string SCR_0260_Projects = "SCR-0260";
        public const string SCR_0310_WorkOrders = "SCR-0310";
        public const string SCR_0320_WorkOrderDetail = "SCR-0320";
        public const string SCR_0330_SafetyChecklist = "SCR-0330";
        public const string SCR_0340_CompletionProof = "SCR-0340";
        public const string SCR_0350_ReviewGate = "SCR-0350";
        public const string SCR_0410_Inventory = "SCR-0410";
        public const string SCR_0420_Reservations = "SCR-0420";
        public const string SCR_0510_Invoices = "SCR-0510";
        public const string SCR_0520_Payments = "SCR-0520";
        public const string SCR_0530_Allocations = "SCR-0530";
        public const string SCR_0540_Ledger = "SCR-0540";
        public const string SCR_0720_Operations = "SCR-0720";
        public const string SCR_0810_Idempotency = "SCR-0810";
        public const string SCR_0820_RateLimit = "SCR-0820";
        public const string SCR_0840_ErrorStandards = "SCR-0840";
    }

    public static class I18n
    {
        public const string I18N_01101 = "i18n.voltflow.01.01_101"; // Hatalı Şifre ile Giriş Reddi (401)
        public const string I18N_01102 = "i18n.voltflow.01.01_102"; // Onaysız Kullanıcı Giriş Reddi (403)
        public const string I18N_01103 = "i18n.voltflow.01.01_103"; // Eksik Email ile Login (400)
        public const string I18N_01104 = "i18n.voltflow.01.01_104"; // Şifresiz Kayıt Denemesi (400)
        public const string I18N_01201 = "i18n.voltflow.01.01_201"; // Master OTP (000000) ile Kayıt (200/202)
        public const string I18N_01202 = "i18n.voltflow.01.01_202"; // Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403
        public const string I18N_01203 = "i18n.voltflow.01.01_203"; // Mükerrer E-posta Kaydı Reddi (400/409)
        public const string I18N_01204 = "i18n.voltflow.01.01_204"; // Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK)
        public const string I18N_01403 = "i18n.voltflow.01.01_403"; // Yönetici Kullanıcı Onayı (VF-01101)
        public const string I18N_01404 = "i18n.voltflow.01.01_404"; // Yetkisiz Kullanıcı Onay Reddi (403)
        public const string I18N_01405 = "i18n.voltflow.01.01_405"; // Yönetici Tarafından Rol Ataması (VF-01101)
        public const string I18N_01406 = "i18n.voltflow.01.01_406"; // Tanımsız Rol Atama Reddi (422)
        public const string I18N_01407 = "i18n.voltflow.01.01_407"; // Var Olmayan Kullanıcıyı Onaylama (404)
        public const string I18N_01301 = "i18n.voltflow.01.01_301"; // Oturum İptal Etme (204)
        public const string I18N_01302 = "i18n.voltflow.01.01_302"; // İptal Edilmiş Token ile Erişim (401)
        public const string I18N_01303 = "i18n.voltflow.01.01_303"; // Token Olmadan Korumalı Endpoint (401)
        public const string I18N_01304 = "i18n.voltflow.01.01_304"; // Geçersiz JWT Token (401)
        public const string I18N_01401 = "i18n.voltflow.01.01_401"; // Şifre Sıfırlama ve Yeni Şifre ile Giriş (200)
        public const string I18N_01402 = "i18n.voltflow.01.01_402"; // Geçersiz Şifre Sıfırlama Token'ı (400)
        public const string I18N_02101 = "i18n.voltflow.02.02_101"; // Müşteri Lead Başlatma ve Aktivasyon
        public const string I18N_02102 = "i18n.voltflow.02.02_102"; // Mükerrer Müşteri Kayıt Reddi (422)
        public const string I18N_02103 = "i18n.voltflow.02.02_103"; // Müşteri Detay ve 404 Koruması (VF-02101)
        public const string I18N_02104 = "i18n.voltflow.02.02_104"; // Müşteri Listesi (200)
        public const string I18N_02105 = "i18n.voltflow.02.02_105"; // Eksik Email ile Müşteri Oluşturma (422)
        public const string I18N_02201 = "i18n.voltflow.02.02_201"; // Teklif Listesi (200)
        public const string I18N_02202 = "i18n.voltflow.02.02_202"; // Var Olmayan Teklif ID (404)
        public const string I18N_02203 = "i18n.voltflow.02.02_203"; // Teklif Taslağı Oluştur ve Kalem Ekle (200)
        public const string I18N_02301 = "i18n.voltflow.02.02_301"; // Kalemsiz Teklif Yayınlama Reddi (400)
        public const string I18N_02302 = "i18n.voltflow.02.02_302"; // Geçersiz Teklif Kalemi Engeli (422)
        public const string I18N_02401 = "i18n.voltflow.02.02_401"; // Teklif Kalem Toplamı, Kabul ve Peşinat
        public const string I18N_02402 = "i18n.voltflow.02.02_402"; // Teklif Reddi ve FSM Koruması (VF-02401)
        public const string I18N_02403 = "i18n.voltflow.02.02_403"; // Teklif Zaman Aşımı Koruması (VF-02401)
        public const string I18N_02404 = "i18n.voltflow.02.02_404"; // Geçersiz Peşinat Ödemesi Reddi (422)
        public const string I18N_02405 = "i18n.voltflow.02.02_405"; // Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)
        public const string I18N_02406 = "i18n.voltflow.02.02_406"; // Red Edilmiş Teklifi Kabul Etme (422)
        public const string I18N_02407 = "i18n.voltflow.02.02_407"; // Süresi Dolmuş Teklifi Kabul Etme (422)
        public const string I18N_02501 = "i18n.voltflow.02.02_501"; // Teklif -> İş Emri Dönüşümü ve Idempotency
        public const string I18N_02502 = "i18n.voltflow.02.02_502"; // Onaysız Tekliften İş Emri Üretim Engeli (422)
        public const string I18N_02601 = "i18n.voltflow.02.02_601"; // Proje ve Faz Yönetimi (VF-02501)
        public const string I18N_02602 = "i18n.voltflow.02.02_602"; // Proje Listesi (200)
        public const string I18N_02603 = "i18n.voltflow.02.02_603"; // Var Olmayan Proje ID (404)
        public const string I18N_03101 = "i18n.voltflow.03.03_101"; // İSG Kontrol Listesi Bariyeri
        public const string I18N_03102 = "i18n.voltflow.03.03_102"; // İş Emri Atama ve Yola Çıkış (VF-03101)
        public const string I18N_03103 = "i18n.voltflow.03.03_103"; // Adreste Bulunamama (NoShow) Raporu (VF-03101)
        public const string I18N_03104 = "i18n.voltflow.03.03_104"; // Atanmamış İş Emrini Yola Çıkarma Engeli
        public const string I18N_03105 = "i18n.voltflow.03.03_105"; // İş Emri Listesi (200)
        public const string I18N_03106 = "i18n.voltflow.03.03_106"; // Var Olmayan İş Emri ID (404)
        public const string I18N_03107 = "i18n.voltflow.03.03_107"; // İş Emri Oluşturma (200)
        public const string I18N_03108 = "i18n.voltflow.03.03_108"; // İş Emri Tam Durum Makinesi (Happy Path)
        public const string I18N_03201 = "i18n.voltflow.03.03_201"; // Zaman Takibi ve Çift Check-In Engeli
        public const string I18N_03202 = "i18n.voltflow.03.03_202"; // Açık Oturumsuz Check-Out Engeli (422)
        public const string I18N_03301 = "i18n.voltflow.03.03_301"; // Beklemeye Alma ve Otomatik Check-Out Telafisi
        public const string I18N_03302 = "i18n.voltflow.03.03_302"; // Sahada Malzeme Tüketimi (VF-03401)
        public const string I18N_03303 = "i18n.voltflow.03.03_303"; // Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422)
        public const string I18N_03401 = "i18n.voltflow.03.03_401"; // Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama
        public const string I18N_03402 = "i18n.voltflow.03.03_402"; // Kanıtsız Tamamlama Engeli (422)
        public const string I18N_03501 = "i18n.voltflow.03.03_501"; // Onay Bariyeri ve Terminal Faturalanmış Durum
        public const string I18N_03502 = "i18n.voltflow.03.03_502"; // İş Emri İptali ve FSM Koruması (VF-03501)
        public const string I18N_03503 = "i18n.voltflow.03.03_503"; // Beklemede Olmayan İş Emri Resume Engeli (422)
        public const string I18N_03504 = "i18n.voltflow.03.03_504"; // Onaysız Faturalama Engeli (422)
        public const string I18N_03505 = "i18n.voltflow.03.03_505"; // İptal Edilmiş İş Emrini Başlatma (422)
        public const string I18N_03506 = "i18n.voltflow.03.03_506"; // Tamamlanan İş Emri Faturalama Onayı
        public const string I18N_04101 = "i18n.voltflow.04.04_101"; // Negatif Stok Bariyeri ve Atomik Geri Alma
        public const string I18N_04102 = "i18n.voltflow.04.04_102"; // Stok Sorgulama ve 404 Doğrulaması (VF-04101)
        public const string I18N_04103 = "i18n.voltflow.04.04_103"; // Pozitif Stok Ayarlama (200)
        public const string I18N_04104 = "i18n.voltflow.04.04_104"; // Sıfır Delta ile Stok Ayarlama (422)
        public const string I18N_04201 = "i18n.voltflow.04.04_201"; // Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201)
        public const string I18N_04202 = "i18n.voltflow.04.04_202"; // Yetersiz Stok Rezervasyonu Reddi
        public const string I18N_04203 = "i18n.voltflow.04.04_203"; // Sıfır/Negatif Stok Rezervasyon Engeli (422)
        public const string I18N_04204 = "i18n.voltflow.04.04_204"; // Negatif Stok Rezervasyonu Engeli (400/422)
        public const string I18N_04205 = "i18n.voltflow.04.04_205"; // Ardışık Rezervasyonlar Müsait Stoğu Azaltır
        public const string I18N_05101 = "i18n.voltflow.05.05_101"; // Müşteri Fatura Listeleme ve Bakiye (VF-05101)
        public const string I18N_05102 = "i18n.voltflow.05.05_102"; // Proje Hakediş Girişi (VF-05101)
        public const string I18N_05103 = "i18n.voltflow.05.05_103"; // Müşteriye Göre Ödeme Listesi (200)
        public const string I18N_05104 = "i18n.voltflow.05.05_104"; // Projeye Göre Billing Listesi (200)
        public const string I18N_05201 = "i18n.voltflow.05.05_201"; // Tahsilat ve Cari Alacak Kaydı (VF-05201)
        public const string I18N_05202 = "i18n.voltflow.05.05_202"; // Sıfır/Eksi Tahsilat Giriş Engeli (422)
        public const string I18N_05203 = "i18n.voltflow.05.05_203"; // Geçersiz Müşteri ID ile Ödeme (422)
        public const string I18N_05204 = "i18n.voltflow.05.05_204"; // Geçersiz Ödeme Yöntemi (422)
        public const string I18N_05301 = "i18n.voltflow.05.05_301"; // Tahsilat Fatura Mahsubu (VF-05301)
        public const string I18N_05302 = "i18n.voltflow.05.05_302"; // Aşırı Mahsup Engeli (VF-05302)
        public const string I18N_05303 = "i18n.voltflow.05.05_303"; // Fatura Bakiyesini Aşan Mahsup Engeli (422)
        public const string I18N_05304 = "i18n.voltflow.05.05_304"; // Var Olmayan Faturaya Mahsup (422)
        public const string I18N_05305 = "i18n.voltflow.05.05_305"; // Sıfır Tutarlı Mahsup (422)
        public const string I18N_05401 = "i18n.voltflow.05.05_401"; // Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401)
        public const string I18N_07201 = "i18n.voltflow.07.07_201"; // Dead-Letter Outbox Kuyruğu (VF-07201)
        public const string I18N_07202 = "i18n.voltflow.07.07_202"; // Yetkisiz Operasyon Erişimi Reddi (403)
        public const string I18N_07203 = "i18n.voltflow.07.07_203"; // Var Olmayan Outbox Replay (404)
        public const string I18N_08101 = "i18n.voltflow.08.08_101"; // Idempotency Key Uyuşmazlığı (409)
        public const string I18N_08201 = "i18n.voltflow.08.08_201"; // Dağıtık Hız Sınırlama Bariyeri (429)
        public const string I18N_08401 = "i18n.voltflow.08.08_401"; // RFC 7807 Problem Details Uyumluluğu
        public const string I18N_08402 = "i18n.voltflow.08.08_402"; // Geçersiz Content-Type (415)
        public const string I18N_08403 = "i18n.voltflow.08.08_403"; // Boş Body ile POST İsteği (400)
        public const string I18N_08404 = "i18n.voltflow.08.08_404"; // Bozuk JSON ile POST İsteği (400)
        public const string I18N_08405 = "i18n.voltflow.08.08_405"; // Var Olmayan Endpoint (404)
        public const string I18N_08406 = "i18n.voltflow.08.08_406"; // Yanlış HTTP Metodu (405)
    }

    public static class FsmTransitions
    {
        public const string Stateless = "N/A (Stateless / Idempotent)";

        public const string FSM_01101 = "UserSession[None -> Failed] (Stateless Auth Guard)"; // Hatalı Şifre ile Giriş Reddi (401)
        public const string FSM_01102 = "AppUser[Registered] (Guard: IsApproved == False)"; // Onaysız Kullanıcı Giriş Reddi (403)
        public const string FSM_01103 = "N/A (Stateless Validation)"; // Eksik Email ile Login (400)
        public const string FSM_01104 = "N/A (Stateless Validation)"; // Şifresiz Kayıt Denemesi (400)
        public const string FSM_01201 = "AppUser[None -> Registered] (Requires Admin Approval)"; // Master OTP (000000) ile Kayıt (200/202)
        public const string FSM_01202 = "AppUser[None -> Unapproved] (Invalid OTP)"; // Geçersiz OTP ile Kayıt -> IsApproved=False & Girişte 403
        public const string FSM_01203 = "N/A (Unique Constraint Guard)"; // Mükerrer E-posta Kaydı Reddi (400/409)
        public const string FSM_01204 = "AppUser[Registered -> Approved]"; // Onaylı Kullanıcı Kaydı ve Başarılı Giriş (200 OK)
        public const string FSM_01403 = "AppUser[PendingApproval -> Active]"; // Yönetici Kullanıcı Onayı (VF-01101)
        public const string FSM_01404 = "AppUser[State Unchanged] (Unauthorized)"; // Yetkisiz Kullanıcı Onay Reddi (403)
        public const string FSM_01405 = "AppUserRole[RoleAssigned]"; // Yönetici Tarafından Rol Ataması (VF-01101)
        public const string FSM_01406 = "N/A (Stateless Role Guard)"; // Tanımsız Rol Atama Reddi (422)
        public const string FSM_01407 = "N/A (Entity Not Found)"; // Var Olmayan Kullanıcıyı Onaylama (404)
        public const string FSM_01301 = "UserSession[Active -> Revoked]"; // Oturum İptal Etme (204)
        public const string FSM_01302 = "UserSession[Revoked] (Access Blocked)"; // İptal Edilmiş Token ile Erişim (401)
        public const string FSM_01303 = "N/A (Stateless JWT Guard)"; // Token Olmadan Korumalı Endpoint (401)
        public const string FSM_01304 = "N/A (Stateless JWT Guard)"; // Geçersiz JWT Token (401)
        public const string FSM_01401 = "PasswordResetToken[Pending -> Redeemed] && AppUser[PasswordUpdated]"; // Şifre Sıfırlama ve Yeni Şifre ile Giriş (200)
        public const string FSM_01402 = "PasswordResetToken[Expired/Invalid] (Rejected)"; // Geçersiz Şifre Sıfırlama Token'ı (400)
        public const string FSM_02101 = "Customer[Lead -> Active]"; // Müşteri Lead Başlatma ve Aktivasyon
        public const string FSM_02102 = "N/A (Unique TaxId/Email Guard)"; // Mükerrer Müşteri Kayıt Reddi (422)
        public const string FSM_02103 = "N/A (Stateless Query)"; // Müşteri Detay ve 404 Koruması (VF-02101)
        public const string FSM_02104 = "N/A (Stateless Query)"; // Müşteri Listesi (200)
        public const string FSM_02105 = "N/A (Stateless Validation)"; // Eksik Email ile Müşteri Oluşturma (422)
        public const string FSM_02201 = "N/A (Stateless Query)"; // Teklif Listesi (200)
        public const string FSM_02202 = "N/A (Entity Not Found)"; // Var Olmayan Teklif ID (404)
        public const string FSM_02203 = "Quote[None -> Draft]"; // Teklif Taslağı Oluştur ve Kalem Ekle (200)
        public const string FSM_02301 = "Quote[Draft -> Issued] (Guard: Items.Count > 0)"; // Kalemsiz Teklif Yayınlama Reddi (400)
        public const string FSM_02302 = "QuoteItem[Validation Guard: Quantity > 0 && UnitPrice >= 0]"; // Geçersiz Teklif Kalemi Engeli (422)
        public const string FSM_02401 = "Quote[Issued -> Accepted] (Guard: DepositPaid >= RequiredDeposit)"; // Teklif Kalem Toplamı, Kabul ve Peşinat
        public const string FSM_02402 = "Quote[Issued -> Rejected] (Reason required)"; // Teklif Reddi ve FSM Koruması (VF-02401)
        public const string FSM_02403 = "Quote[Issued -> Expired] (TimeWindow Guard)"; // Teklif Zaman Aşımı Koruması (VF-02401)
        public const string FSM_02404 = "QuoteDeposit[Validation Guard: Deposit > Total]"; // Geçersiz Peşinat Ödemesi Reddi (422)
        public const string FSM_02405 = "Quote[FSM Conflict: Already Accepted -> Rejected]"; // Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)
        public const string FSM_02406 = "Quote[FSM Conflict: Rejected -> Accept Forbidden]"; // Red Edilmiş Teklifi Kabul Etme (422)
        public const string FSM_02407 = "Quote[FSM Conflict: Expired -> Accept Forbidden]"; // Süresi Dolmuş Teklifi Kabul Etme (422)
        public const string FSM_02501 = "Quote[Accepted] -> WorkOrder[Open] (Idempotent Conversion)"; // Teklif -> İş Emri Dönüşümü ve Idempotency
        public const string FSM_02502 = "Quote[Draft/Issued] -> WorkOrder[Forbidden: Not Accepted]"; // Onaysız Tekliften İş Emri Üretim Engeli (422)
        public const string FSM_02601 = "Project[Created] -> ProjectPhase[Scheduled]"; // Proje ve Faz Yönetimi (VF-02501)
        public const string FSM_02602 = "N/A (Stateless Query)"; // Proje Listesi (200)
        public const string FSM_02603 = "N/A (Entity Not Found)"; // Var Olmayan Proje ID (404)
        public const string FSM_03101 = "WorkOrder[Open] (Safety Checklist Incomplete -> Start Forbidden)"; // İSG Kontrol Listesi Bariyeri
        public const string FSM_03102 = "WorkOrder[Open -> Assigned -> EnRoute]"; // İş Emri Atama ve Yola Çıkış (VF-03101)
        public const string FSM_03103 = "WorkOrder[Assigned/EnRoute -> NoShow]"; // Adreste Bulunamama (NoShow) Raporu (VF-03101)
        public const string FSM_03104 = "WorkOrder[Open -> EnRoute Forbidden: Must Assign First]"; // Atanmamış İş Emrini Yola Çıkarma Engeli
        public const string FSM_03105 = "N/A (Stateless Query)"; // İş Emri Listesi (200)
        public const string FSM_03106 = "N/A (Stateless Operation)"; // Var Olmayan İş Emri ID (404)
        public const string FSM_03107 = "N/A (Stateless Operation)"; // İş Emri Oluşturma (200)
        public const string FSM_03108 = "N/A (Stateless Operation)"; // İş Emri Tam Durum Makinesi (Happy Path)
        public const string FSM_03201 = "WorkOrder[Assigned/EnRoute -> InProgress] (Guard: SafetyCompleted)"; // Zaman Takibi ve Çift Check-In Engeli
        public const string FSM_03202 = "WorkOrderTimeEntry[Active -> Stopped] && DurationCalculated"; // Açık Oturumsuz Check-Out Engeli (422)
        public const string FSM_03301 = "WorkOrder[InProgress -> OnHold] (Guard: HoldReason != null)"; // Beklemeye Alma ve Otomatik Check-Out Telafisi
        public const string FSM_03302 = "WorkOrder[OnHold -> InProgress]"; // Sahada Malzeme Tüketimi (VF-03401)
        public const string FSM_03303 = "WorkOrder[Closed -> PutOnHold Forbidden]"; // Tamamlanmış İş Emrine Malzeme Ekleme Engeli (422)
        public const string FSM_03401 = "WorkOrder[InProgress -> Completed] (Guard: Signature != null && Photo != null)"; // Kanıtsız Tamamlama Engeli ve Kanıtlı Tamamlama
        public const string FSM_03402 = "WorkOrder[FSM Violation: Complete without InProgress -> Rejected]"; // Kanıtsız Tamamlama Engeli (422)
        public const string FSM_03501 = "WorkOrder[Completed -> ReadyForBilling] (Gate Approved)"; // Onay Bariyeri ve Terminal Faturalanmış Durum
        public const string FSM_03502 = "WorkOrder[ReadyForBilling -> Invoiced]"; // İş Emri İptali ve FSM Koruması (VF-03501)
        public const string FSM_03503 = "WorkOrder[!Completed && !Invoiced -> Cancelled]"; // Beklemede Olmayan İş Emri Resume Engeli (422)
        public const string FSM_03504 = "WorkOrder[FSM Violation: Invoice without ReadyForBilling -> Rejected]"; // Onaysız Faturalama Engeli (422)
        public const string FSM_03505 = "WorkOrder[Completed/Invoiced -> Cancel Forbidden]"; // İptal Edilmiş İş Emrini Başlatma (422)
        public const string FSM_03506 = "N/A (Stateless Operation)"; // Tamamlanan İş Emri Faturalama Onayı
        public const string FSM_04101 = "StockLevel[WarehouseStockAdjusted]"; // Negatif Stok Bariyeri ve Atomik Geri Alma
        public const string FSM_04102 = "StockLevel[Guard: NegativeStockRejected]"; // Stok Sorgulama ve 404 Doğrulaması (VF-04101)
        public const string FSM_04103 = "N/A (Stateless Query)"; // Pozitif Stok Ayarlama (200)
        public const string FSM_04104 = "WarehouseTransfer[StockReserved -> StockMoved]"; // Sıfır Delta ile Stok Ayarlama (422)
        public const string FSM_04201 = "StockReservation[Pending -> Reserved]"; // Stok Rezervasyonu ve Kullanılabilir Azalma (VF-04201)
        public const string FSM_04202 = "StockReservation[Reserved -> Released]"; // Yetersiz Stok Rezervasyonu Reddi
        public const string FSM_04203 = "StockReservation[Guard: InsufficientStockRejected]"; // Sıfır/Negatif Stok Rezervasyon Engeli (422)
        public const string FSM_04204 = "N/A (Validation Guard: Negatif Stok Rezervasyonu Engeli (400/422))"; // Negatif Stok Rezervasyonu Engeli (400/422)
        public const string FSM_04205 = "N/A (Stateless Operation)"; // Ardışık Rezervasyonlar Müsait Stoğu Azaltır
        public const string FSM_05101 = "SalesInvoice[Draft -> Issued]"; // Müşteri Fatura Listeleme ve Bakiye (VF-05101)
        public const string FSM_05102 = "SalesInvoice[Guard: ZeroAmountRejected]"; // Proje Hakediş Girişi (VF-05101)
        public const string FSM_05103 = "N/A (Stateless Query)"; // Müşteriye Göre Ödeme Listesi (200)
        public const string FSM_05104 = "N/A (Stateless Query)"; // Projeye Göre Billing Listesi (200)
        public const string FSM_05201 = "CustomerPayment[Pending -> Received]"; // Tahsilat ve Cari Alacak Kaydı (VF-05201)
        public const string FSM_05202 = "CustomerPayment[Guard: NegativePaymentRejected]"; // Sıfır/Eksi Tahsilat Giriş Engeli (422)
        public const string FSM_05203 = "N/A (Stateless Operation)"; // Geçersiz Müşteri ID ile Ödeme (422)
        public const string FSM_05204 = "N/A (Stateless Operation)"; // Geçersiz Ödeme Yöntemi (422)
        public const string FSM_05301 = "SalesInvoice[Issued -> PartiallyPaid/Paid] (Payment Allocated)"; // Tahsilat Fatura Mahsubu (VF-05301)
        public const string FSM_05302 = "PaymentAllocation[Guard: AllocationExceedsInvoiceBalance]"; // Aşırı Mahsup Engeli (VF-05302)
        public const string FSM_05303 = "PaymentAllocation[Guard: AllocationExceedsPaymentAmount]"; // Fatura Bakiyesini Aşan Mahsup Engeli (422)
        public const string FSM_05304 = "N/A (Stateless Operation)"; // Var Olmayan Faturaya Mahsup (422)
        public const string FSM_05305 = "N/A (Stateless Operation)"; // Sıfır Tutarlı Mahsup (422)
        public const string FSM_05401 = "CustomerLedgerEntry[Posted]"; // Mükerrer / Eşzamanlı Mahsup Engeli (VF-05401)
        public const string FSM_07201 = "OutboxMessage[Pending -> Dispatched / Failed]"; // Dead-Letter Outbox Kuyruğu (VF-07201)
        public const string FSM_07202 = "OutboxMessage[Failed -> Retried / PoisonQueue]"; // Yetkisiz Operasyon Erişimi Reddi (403)
        public const string FSM_07203 = "N/A (Stateless Operation)"; // Var Olmayan Outbox Replay (404)
        public const string FSM_08101 = "IdempotencyRecord[InFlight -> Completed / 409 Conflict]"; // Idempotency Key Uyuşmazlığı (409)
        public const string FSM_08201 = "RateLimitBucket[Incremented / Throttled 429]"; // Dağıtık Hız Sınırlama Bariyeri (429)
        public const string FSM_08401 = "N/A (RFC 7807 Error Response Formatting)"; // RFC 7807 Problem Details Uyumluluğu
        public const string FSM_08402 = "N/A (Validation Exception Mapping 422)"; // Geçersiz Content-Type (415)
        public const string FSM_08403 = "N/A (Unauthorized Access Mapping 401)"; // Boş Body ile POST İsteği (400)
        public const string FSM_08404 = "N/A (Forbidden Access Mapping 403)"; // Bozuk JSON ile POST İsteği (400)
        public const string FSM_08405 = "N/A (Resource Not Found Mapping 404)"; // Var Olmayan Endpoint (404)
        public const string FSM_08406 = "N/A (Global Unhandled Exception 500)"; // Yanlış HTTP Metodu (405)
    }
}
