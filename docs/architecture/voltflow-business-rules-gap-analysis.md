# Voltflow Is Kurallari Bosluk Analizi

## Amac

Bu belge, mevcut .NET kodu, EF Core modeli ve ilk sprint planini karsilastirarak uygulama koduna gecmeden once netlestirilmesi gereken is kurallarini listeler.

Kurallar iki gruba ayrilir:

- **DDL ile kesinlesmis:** Schema tarafinda constraint, state veya iliski ile zaten tanimli. Uygulama bunu uygulamali; yeniden karar verilmemeli.
- **Karar bekleyen:** Schema ve mevcut kod ayni davranisi zorunlu kilmiyor. Kullanici karari olmadan implement edilmemeli.

## 1. DDL ile kesinlesmis kurallar

### Customer

- Customer tax number benzersizdir.
- Candidate customer tek bir party kaydina baglidir.
- Bir candidate yalnizca bir kez customer'a donusturulebilir.
- Bir customer yalnizca bir conversion kaydinda hedef olabilir.
- Conversion candidate party ile customer party ayni olamaz.
- Conversion actor ve zaman bilgisi tutulur.
- Customer, party kaydina baglidir; uygulama conversion sirasinda party iliskisini atomik kurmalidir.

### Quote

- Quote state degerleri: `DRAFT`, `ISSUED`, `ACCEPTED`, `REJECTED`, `EXPIRED`.
- `valid_until`, `issue_date` tarihinden once olamaz.
- Grand total formulu: `subtotal - discount_total + vat_total`.
- Material, labor ve service satirlarinda miktar sifirdan buyuk; fiyatlar negatif olamaz.
- Quote item'lari quote silinince cascade silinir.
- Candidate quote ile canonical quote arasinda tekil conversion vardir.
- Issue, accept, reject ve expire actor/zaman bilgisiyle kaydedilmelidir.

### Work order

- Work order state degerleri: `OPEN`, `ASSIGNED`, `IN_PROGRESS`, `COMPLETED`, `INVOICED`, `CANCELLED`.
- Assignment employee uzerinden yapilir ve ayni employee ayni work order'a iki kez atanamaz.
- Completion kaydi tektir ve customer signature name zorunludur.
- Material quantity, labor hours ve parasal alanlar negatif veya sifir olamaz.
- Quote-work order baglantisi cift olarak tekrar edemez.
- Invoice baglantisinda allocation amount pozitif olmalidir.

### Inventory

- Stock balance `(stock_location, material)` ciftinde tektir.
- Stock quantity negatif olamaz.
- Stock movement quantity pozitif olmalidir; hareket yonu lookup kaydindan gelir.
- Movement kaydi onceki ve sonraki bakiyeyi saklar.
- Material barcode unique'dir.
- Material VAT orani 0 ile 100 arasindadir.
- Material minimum quantity negatif olamaz.
- Price amount ve stock unit cost negatif olamaz.

### Finance

- Invoice toplam formulleri schema constraint'leriyle korunur.
- Invoice due date, invoice date'ten once olamaz.
- Payment amount pozitif olmalidir.
- Payment allocation amount pozitif olmalidir.
- Ayni payment ayni invoice veya progress billing'e iki allocation kaydi acamaz.
- Customer ledger amount pozitif, balance_after negatif olamaz.

### Reminder, audit ve idempotency

- Audit event actor, entity ve JSON event data ile kaydedilir.
- Execution guard `(scope, idempotency_key)` ciftinde tektir.
- Execution guard state degerleri `PENDING`, `RESOLVED`, `ORPHANED` olabilir.
- Reminder completed veya dismissed durumuna gecince actor, zaman ve not tutulur.
- Document delivery pending, sent veya failed sonucuyla izlenir.

## 2. Karar bekleyen bosluklar

Her baslik tamamlanmadan ilgili application service ve endpoint'i production-ready sayilmayacak.

### A. Kimlik, rol ve yetki

**Mevcut durum:** JWT ve rol claim temeli var. DDL'de application role ve employee role ayridir.

**Netlestirilecekler:**

1. Sistem rolleri hangi endpoint'lere erisebilir?
2. `Admin`, `Manager`, `Technician`, `Viewer` sistem rolleri ile `employee_roles` arasindaki fark nedir?
3. Technician kendi atandigi work order disinda kayit gorebilir mi?
4. Fiyat, maliyet, payment ve ledger bilgilerini hangi roller gorebilir?
5. Role assignment'i kim yapabilir?
6. Kullanici pasiflestirme ve session revoke kurali nedir?

**Guvenli varsayilan onerisi:** Auth gerektiren tum is endpoint'leri; yazma islemleri `Admin` veya `Manager`; technician yalnizca atandigi work order; viewer salt-okuma. Bu onerinin kabul edilmesi gerekir, otomatik uygulanmamistir.

### B. Customer lifecycle

**Mevcut durum:** Kodda basit `Customer` CRUD var. DDL'de candidate, walk-in, conversion, archive ve note yapilari var.

**Netlestirilecekler:**

1. Candidate hangi kaynaklardan olusur?
2. Conversion icin zorunlu alanlar hangileridir: telefon, adres, vergi bilgileri, customer type?
3. Ayni tax number mevcutsa conversion reddedilir mi, mevcut customer'a birlestirme mi yapilir?
4. Conversion geri alinabilir mi?
5. Walk-in customer kalici customer'a nasil donusur?
6. Candidate archive ne zaman ve hangi nedenle olusur?
7. Customer silme yerine her zaman inactive mi yapilir?

**Implementasyon sonucu:** Conversion tek transaction icinde party, customer, conversion ve archive/notes davranisini belirlemelidir.

### C. Quote lifecycle

**Mevcut durum:** Domain state gecisleri var; DDL'deki ayrik item, issue, accept, reject ve expire tablolarinin tam karsiligi yok.

**Netlestirilecekler:**

1. Draft quote issue edilebilmesi icin en az bir item zorunlu mu?
2. Issue sonrasi fiyat, indirim ve item degistirilebilir mi?
3. Discount toplam limiti nedir; subtotal'i asabilir mi?
4. Accept/reject islemini firma kullanicisi mi, musteri adi ile kayit mi yapar?
5. Reject reason zorunlu mu? Mevcut DDL zorunlu oldugunu gosteriyor.
6. Expire otomatik worker ile mi, kullanici islemiyle mi olur?
7. Accepted quote tekrar reject veya expire edilebilir mi?
8. Quote numarasi sequence ile mi uretilir, yoksa zaman tabanli olamaz mi?

**Implementasyon sonucu:** State machine, actor bilgisi ve total hesaplari tek application use-case'lerinde atomik olmali.

### D. Quote -> WorkOrder

**Mevcut durum:** Iki domain modeli bagimsiz. DDL'de `quote_work_order_links` var.

**Netlestirilecekler:**

1. Yalnizca accepted quote work order'a donusebilir mi?
2. Bir quote'tan birden fazla work order acilabilir mi?
3. Item'lar work order'a otomatik kopyalanir mi?
4. Quote toplamindan farkli work order fiyati olabilir mi?
5. Work order olusunca quote hangi durumda kalir?
6. Partial service icin quote item bazli work order desteklenecek mi?
7. Link silinebilir mi, yoksa audit kaydi olarak kalici mi?

**Implementasyon sonucu:** Quote kabulunden work order olusumuna kadar tek transaction ve idempotency key kullanilmalidir.

### E. Work order operasyonu

**Mevcut durum:** Domain'de `Assign`, `Start`, `Complete`, `Invoice` var; status gecisleri fazla gevsek ve technician/employee parametresi yok.

**Netlestirilecekler:**

1. Gecerli gecisler tam olarak hangileri?
2. `OPEN -> COMPLETED` gibi ara adimlar yasak mi?
3. `CANCELLED` hangi durumlarda ve kim tarafindan uygulanabilir?
4. Atama olmadan start veya complete edilebilir mi?
5. Birden fazla employee atanabilir mi?
6. Completion sonrasi material/labor girisi yasak mi?
7. Customer signature zorunlu mu?
8. Invoiced work order tekrar tamamlanabilir veya iptal edilebilir mi?

**Implementasyon sonucu:** DDL state setindeki `CANCELLED` dahil edilerek explicit transition table/testleri yazilmalidir.

### F. Inventory ve stock movement

**Mevcut durum:** Kodda tek `MaterialStock` ve `Adjust/Reserve/Release` var; DDL location/material/movement ledger modelidir.

**Netlestirilecekler:**

1. Stok hangi location'da tutulur?
2. IN, OUT ve NEUTRAL hareketlerinin bakiye etkisi nedir?
3. Negative stock her durumda yasak mi?
4. Reservation hangi olayda olusur: quote, work order, planlama?
5. Work order material kullanimi reservation'i otomatik dusurur mu?
6. Kullanilmayan reservation ne zaman release edilir?
7. Ortalama maliyet, son alim maliyeti veya manuel unit cost mu kullanilir?
8. Low-stock esigi material.minimum_quantity mi?
9. Stock movement silinebilir mi, yoksa correction movement mi acilir?

**Implementasyon sonucu:** Her hareket balance lock/transaction icinde ledger kaydi ve previous/new quantity ile yazilmalidir.

### G. Project, progress billing ve invoice

**Mevcut durum:** Basit Project/BillingEntry modeli var; DDL progress billing, sales invoice ve invoice line modeli tarif ediyor.

**Netlestirilecekler:**

1. Project phase tamamlanma kosulu nedir?
2. Progress billing requested, approved, deduction ve net payable nasil hesaplanir?
3. Approval kim tarafindan yapilir?
4. Invoice ne zaman olusur: work order completion, approved progress billing veya manuel?
5. Invoice state'leri nelerdir?
6. Invoice edit edilebilir mi, yoksa credit/correction belgesi mi kullanilir?
7. Work order birden fazla invoice'a bolunebilir mi?
8. Partial payment ve overpayment nasil ele alinir?

**Implementasyon sonucu:** Invoice, payment, allocation ve ledger tek transaction boundary icinde tasarlanmalidir.

### H. Payment ve customer ledger

**Netlestirilecekler:**

1. Payment allocation otomatik mi manuel mi?
2. Otomatikse FIFO hangi belge tarihine gore yapilir?
3. Payment invoice ve progress billing'e ayni anda dagitilabilir mi?
4. Unallocated payment tutulabilir mi?
5. Iade/ters kayit nasil yapilir?
6. Ledger balance `DEBIT/CREDIT` yonlerine gore nasil guncellenir?
7. Balance snapshot mi, yeniden hesaplanabilir projection mi?

### I. Audit, idempotency ve actor

**Netlestirilecekler:**

1. Hangi islemler audit zorunlu: state transition, money, stock, auth, role, customer data?
2. Event data icinde hangi alanlar saklanir; parola/token kesinlikle saklanmaz.
3. Idempotency scope endpoint mi aggregate mi?
4. Ayni key farkli request hash ile gelirse hangi hata doner?
5. PENDING execution guard timeout ve ORPHANED gecisi nasil olur?
6. Audit kaydi transaction rollback'te geri alinacak mi?

### J. Reminder, delivery ve worker

**Netlestirilecekler:**

1. Reminder tipleri ve tekrar kurali nedir?
2. Due payment reminder ne zaman olusur?
3. Overdue reminder kac kez denenir?
4. Dismiss ile complete arasindaki anlam farki nedir?
5. Email/document delivery retry backoff ve max retry sayisi nedir?
6. Provider failure durumunda kayit hangi state'e gecer?
7. Worker ayni reminder'i iki kez calistirmayi nasil engeller?

## 3. Tamamlama sirasi

Karar bagimliligini azaltan sira:

1. Auth role-permission matrisi ve actor kurali
2. Customer candidate/conversion kurallari
3. Quote state machine ve quote-to-work-order kurallari
4. Work order assignment/completion/cancellation kurallari
5. Inventory movement/reservation/cost kurallari
6. Invoice/progress billing/payment allocation kurallari
7. Audit ve idempotency politikasi
8. Reminder/delivery worker kurallari
9. Her akisin integration testleri ve API authorization testleri

## 4. Cevap formati

Uygulama kararlarini hizli kilitlemek icin asagidaki numaralari cevaplamak yeterlidir:

- **A:** Rol yetkileri: oneriyi kabul / degistir.
- **B:** Customer conversion: duplicate tax number davranisi, rollback, archive.
- **C:** Quote: issue/accept/reject/expire ve numara kurali.
- **D:** Quote -> WorkOrder: bir quote kac work order, item kopyalama.
- **E:** WorkOrder: state gecisleri, assignment ve completion.
- **F:** Inventory: movement yonleri, reservation, cost, negative stock.
- **G/H:** Invoice/payment/ledger: invoice tetigi, allocation, overpayment, iade.
- **I:** Audit/idempotency: zorunlu event'ler ve request key davranisi.
- **J:** Reminder/worker: retry, scheduling, dismissal/completion.

## 5. Onaylanmis kararlar

Asagidaki kararlar proje kurali olarak kabul edilmistir ve A-D implementation diliminin kaynagidir.

### A. Auth ve roller

- `Admin`: tum erisim.
- `Manager`: musteri, teklif, is emri, stok ve fatura islemleri.
- `Technician`: kendisine atanmis is emirleri ve ilgili malzeme kullanimi.
- `Viewer`: salt-okuma.
- Role assignment yalnizca `Admin` tarafindan yapilir.
- `employee_roles`, sistem rollerinden ayri tutulur.
- `/health` disinda API endpoint'leri authentication gerektirir.
- Parola ve token audit kaydina yazilmaz.

### B. Customer conversion

- Candidate tek seferlik Customer'a donusur.
- Ayni vergi numarasi varsa conversion reddedilir.
- Conversion geri alinamaz.
- Candidate conversion sonrasinda archive edilir.
- Party, customer, conversion ve archive islemleri tek transaction icindedir.
- Customer fiziksel olarak silinmez, inactive yapilir.

### C. Quote lifecycle

Gecerli state gecisleri:

```text
DRAFT -> ISSUED
ISSUED -> ACCEPTED
ISSUED -> REJECTED
ISSUED -> EXPIRED
```

- Issue icin en az bir item zorunludur.
- Issue sonrasinda fiyat ve item degistirilemez.
- Reject reason zorunludur.
- Expire otomatik worker tarafindan yapilir.
- Accepted, rejected veya expired quote tekrar acilamaz.
- Quote numarasi sequence ile uretilir.
- Total hesaplari server-side yapilir.

### D. Quote -> WorkOrder

- Yalnizca `ACCEPTED` quote work order'a donusebilir.
- Bir quote yalnizca bir work order uretir.
- Quote item'lari work order item snapshot'larina kopyalanir.
- Quote-work order link'i silinmez.
- Donusum idempotent'tir.
- Quote kabulunden work order olusumuna kadar islem tek transaction icindedir.

### E. Uygulanan finance ve reminder kurallari

- Payment allocation payment'in unallocated amount'ini asamaz.
- Invoice allocation invoice'in remaining amount'ini asamaz.
- Ayni payment ayni invoice'a ikinci kez allocate edilemez.
- Progress billing net payable degeri `approved - deduction` olarak hesaplanir.
- Reminder pending, completed ve dismissed lifecycle durumlarini tasir.
- Reminder delivery attempt sayisi ve sonraki deneme zamani persisted olarak tutulur.
- Invoice allocation ve payment ledger yazimlari transaction siniri icindedir.
- Yeni entity kayitlarinda authenticated actor `CreatedByUserId` metadata'si EF persistence katmaninda otomatik damgalanir.
- Kritik lookup degerleri `ReferenceValue` katalogunda type/code/name ile idempotent seed edilir.
- Her API request'i operation id, parent operation, endpoint, screen, action, outcome ve duration ile izlenir.
- Entity create/update/delete degisiklikleri actor, endpoint ve operation id ile redacted audit event olarak kaydedilir.
- Request body, parola, token ve secret degerleri audit katmaninda plaintext tutulmaz.
- Execution guvenligi pattern'i: idempotency key/request fingerprint ile inbox-style deduplication, fixed-window rate limiting, distributed tracing ve audit trail birlikte kullanilir.
- Audit log enforcement mekanizmasi degildir; duplicate ve quota bloklama execution guard/rate limiter tarafinda yapilir.
- Basarili idempotent isteklerde onceki response replay edilir; handler tekrar calistirilmaz.
- Audit event ile outbox mesaji ayni database transaction'inda yazilir.
- Standart tracing icin operation audit kaydina ek olarak .NET `ActivitySource` kullanilir.
- Her entity degisikligi before/after snapshot ve changed-fields serialization ile data lineage zincirine baglanir.
- Kaynak zinciri operation id, parent operation id, actor, endpoint, screen ve action alanlariyla korunur.
- API hata sozlesmesi RFC 9457 Problem Details kullanir; istemciye stack trace veya hassas exception detayi donmez.
- Beklenen business failure'lar error code ve uygun HTTP status ile, beklenmeyen failure'lar generic `internal_error` ile doner.
- Yeni register kullanicisi Admin onayina kadar pending kalir ve token alamaz; onay sonrasi ilk rol `Viewer` olur.
- Onaylanmis her `AppUser` employee kabul edilir; Technician yalnizca `AssignedUserId` kendi user id'si olan WorkOrder kayitlarini gorur.
- Admin approval sonrasi `Viewer` ilk roldur; Admin, onayli user'a ek sistem rolleri atayabilir.
- Quote rejection reason Quote aggregate'inda kalici tutulur.
- Audit serialization'da email, telefon, vergi numarasi ve adres alanlari KVKK kapsaminda redacted edilir; teknik outbox/guard entity'leri business snapshot'ina dahil edilmez.
- Outbox hatalari bes denemeye kadar exponential backoff ile tekrar edilir; besinci basarisizlik `DeadLetter` durumuna tasinir.
- Testler benzersiz fixture verisi kullanir; global tablo sayilarina dayali assertion kullanmaz.
- Her domain entity update'inde `Version` concurrency token'i artar; eski version ile gelen ikinci update `409 concurrency_conflict` olarak reddedilir.
- `RESOLVED` guard kalici duplicate kanitidir; `PENDING` lease suresi dolarsa temizlenir ve retry edilebilir; failure `ORPHANED` durumuna tasinir.
