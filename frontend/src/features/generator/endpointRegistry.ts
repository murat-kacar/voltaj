export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export type ParamType = 'text' | 'number' | 'boolean' | 'date' | 'datetime' | 'enum' | 'fk' | 'json'

export interface EndpointField {
  name: string
  label: string
  type: ParamType
  required?: boolean
  options?: { value: string | number | boolean; label: string; color?: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' }[]
  fkEntity?: 'Customer' | 'User' | 'Warehouse' | 'Project' | 'Quote' | 'WorkOrder' | 'Site'
  helperText?: string
}

export interface EndpointDefinition {
  id: string
  method: HttpMethod
  path: string
  title: string
  category: string
  description: string
  pathParams?: EndpointField[]
  queryParams?: EndpointField[]
  bodyFields?: EndpointField[]
  generateMock?: (lookups?: { customerId?: string; userId?: string; warehouseId?: string; projectId?: string }) => {
    path?: Record<string, string>
    query?: Record<string, string | number | boolean>
    body?: Record<string, unknown>
  }
}

export interface EndpointCategory {
  id: string
  label: string
}

export const ENDPOINT_CATEGORIES: EndpointCategory[] = [
  { id: 'all', label: 'Tüm Endpoint\'ler' },
  { id: 'services', label: 'Hizmetler & İş Emirleri' },
  { id: 'crm', label: 'Müşteriler & CRM' },
  { id: 'finance', label: 'Finans & Faturalar' },
  { id: 'retail', label: 'POS & Kasa' },
  { id: 'inventory', label: 'Envanter & Stok' },
  { id: 'reminders', label: 'Hatırlatıcılar' },
  { id: 'identity', label: 'Kimlik & Yetki' },
  { id: 'system', label: 'Sistem & Denetim' },
]

function rand(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min
}

function randId(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

export const ALL_ENDPOINTS: EndpointDefinition[] = [
  // ==========================================================================
  // 1. SERVICES (Hizmetler)
  // ==========================================================================
  {
    id: 'svc-list',
    method: 'GET',
    path: '/api/services',
    title: 'Hizmetleri Listele',
    category: 'services',
    description: 'Sistemdeki hizmet / iş emirlerini sayfalayarak listeler.',
    queryParams: [
      { name: 'limit', label: 'Limit', type: 'number', helperText: 'Örn: 20' },
      { name: 'offset', label: 'Offset', type: 'number', helperText: 'Örn: 0' },
    ],
    generateMock: () => ({
      query: { limit: 20, offset: 0 },
    }),
  },
  {
    id: 'svc-create',
    method: 'POST',
    path: '/api/services',
    title: 'Yeni Taslak Hizmet Oluştur',
    category: 'services',
    description: 'Müşteri için yeni bir taslak hizmet (teklif / iş emri başlangıcı) oluşturur.',
    bodyFields: [
      { name: 'customerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'title', label: 'Hizmet Başlığı', type: 'text', required: true },
    ],
    generateMock: (l) => ({
      body: {
        customerId: l?.customerId || '572a1548-dbdc-4b71-9c60-a2ea03d6d03f',
        title: `Trafo Bakım ve Yıllık Periyodik Test (${rand(100, 999)})`,
      },
    }),
  },
  {
    id: 'svc-get',
    method: 'GET',
    path: '/api/services/{id}',
    title: 'Hizmet Detayını Getir',
    category: 'services',
    description: 'Belirtilen ID değerine sahip hizmeti getirir.',
    pathParams: [
      { name: 'id', label: 'Hizmet ID', type: 'fk', fkEntity: 'WorkOrder', required: true },
    ],
    generateMock: () => ({
      path: { id: '313e4fc8-9f33-4f96-be0d-b45cb0ca1ff3' },
    }),
  },

  // ==========================================================================
  // 3. CUSTOMERS & CRM
  // ==========================================================================
  {
    id: 'cust-list',
    method: 'GET',
    path: '/api/customers',
    title: 'Müşterileri Listele',
    category: 'crm',
    description: 'Müşteri ve potansiyel (lead) firma kayıtlarını listeler.',
    queryParams: [
      { name: 'search', label: 'Arama (İsim/Vergi No)', type: 'text' },
      { name: 'activeOnly', label: 'Sadece Aktifler', type: 'boolean' },
    ],
    generateMock: () => ({
      query: { activeOnly: true },
    }),
  },
  {
    id: 'cust-create',
    method: 'POST',
    path: '/api/customers',
    title: 'Yeni Müşteri Oluştur',
    category: 'crm',
    description: 'Kurumsal veya bireysel yeni müşteri kartı açar.',
    bodyFields: [
      { name: 'fullName', label: 'Müşteri / Şirket Ünvanı', type: 'text', required: true },
      { name: 'email', label: 'E-Posta Adresi', type: 'text', required: true },
      { name: 'phone', label: 'Telefon Numarası', type: 'text', required: true },
      { name: 'taxNumber', label: 'Vergi No / T.C. Kimlik', type: 'text', required: true },
    ],
    generateMock: () => {
      const id = rand(100, 999)
      return {
        body: {
          fullName: `Akdeniz Elektrik Mühendislik Ltd. Şti. (${id})`,
          email: `muhasebe${id}@akdeniz-elk.com.tr`,
          phone: `+90 532 555 ${rand(10, 99)} ${rand(10, 99)}`,
          taxNumber: `1234567${rand(100, 999)}`,
        },
      }
    },
  },
  {
    id: 'cust-site-create',
    method: 'POST',
    path: '/api/customers/{id}/sites',
    title: 'Müşteriye Saha / Tesis Ekle',
    category: 'crm',
    description: 'Müşterinin fabrikası, deposu veya şubesi için saha lokasyonu tanımlar.',
    pathParams: [
      { name: 'id', label: 'Müşteri ID', type: 'fk', fkEntity: 'Customer', required: true },
    ],
    bodyFields: [
      { name: 'name', label: 'Saha / Tesis Adı', type: 'text', required: true },
      { name: 'address', label: 'Tam Adres', type: 'text', required: true },
      { name: 'contactPerson', label: 'Yetkili Kişi', type: 'text' },
      { name: 'contactPhone', label: 'Yetkili Telefon', type: 'text' },
    ],
    generateMock: (l) => ({
      path: { id: l?.customerId || '572a1548-dbdc-4b71-9c60-a2ea03d6d03f' },
      body: {
        name: 'Merkez Fabrika - Trafo Binası',
        address: 'Organize Sanayi Bölgesi 4. Cadde No:12 Nilüfer / Bursa',
        contactPerson: 'Serkan Yılmaz (Tesis Müdürü)',
        contactPhone: '+90 533 111 22 33',
      },
    }),
  },

  // ==========================================================================
  // 4. FINANCE & INVOICES (Finans & Faturalar)
  // ==========================================================================
  {
    id: 'pay-list',
    method: 'GET',
    path: '/api/payments',
    title: 'Tahsilat ve Ödemeleri Listele',
    category: 'finance',
    description: 'Gerçekleşen müşteri tahsilatlarını listeler.',
    queryParams: [
      { name: 'customerId', label: 'Müşteri Filtresi', type: 'fk', fkEntity: 'Customer' },
      { name: 'limit', label: 'Limit', type: 'number' },
    ],
    generateMock: () => ({
      query: { limit: 20 },
    }),
  },
  {
    id: 'pay-create',
    method: 'POST',
    path: '/api/payments',
    title: 'Müşteri Ödemesi / Tahsilat Kaydet',
    category: 'finance',
    description: 'Nakit, Kart veya Banka Havalesi yoluyla müşteri ödemesi kaydeder.',
    bodyFields: [
      { name: 'customerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'amount', label: 'Ödenen Tutar (₺)', type: 'number', required: true },
      {
        name: 'paymentMethod',
        label: 'Ödeme Yöntemi',
        type: 'enum',
        required: true,
        options: [
          { value: 'BankTransfer', label: 'Banka Havalesi / EFT', color: 'info' },
          { value: 'Card', label: 'Kredi Kartı', color: 'primary' },
          { value: 'Cash', label: 'Nakit Tahsilat', color: 'success' },
        ],
      },
      { name: 'paymentDate', label: 'Ödeme Tarihi', type: 'date', required: true },
    ],
    generateMock: (l) => ({
      body: {
        customerId: l?.customerId || '572a1548-dbdc-4b71-9c60-a2ea03d6d03f',
        amount: rand(5, 50) * 1000,
        paymentMethod: 'BankTransfer',
        paymentDate: new Date().toISOString().slice(0, 10),
      },
    }),
  },
  {
    id: 'pay-invoices',
    method: 'GET',
    path: '/api/payments/invoices',
    title: 'Satış Faturalarını Listele',
    category: 'finance',
    description: 'Kesilen satış faturalarını ve bakiye durumlarını listeler.',
    queryParams: [
      { name: 'limit', label: 'Limit', type: 'number' },
    ],
    generateMock: () => ({
      query: { limit: 20 },
    }),
  },

  // ==========================================================================
  // 5. POS & RETAIL (Hızlı Satış & Kasa)
  // ==========================================================================
  {
    id: 'pos-shift-current',
    method: 'GET',
    path: '/api/cash-shifts/current',
    title: 'Aktif Kasa Vardiyasını Getir',
    category: 'retail',
    description: 'Giriş yapan personelin şu an açık olan kasa vardiyasını döner.',
    generateMock: () => ({}),
  },
  {
    id: 'pos-shift-open',
    method: 'POST',
    path: '/api/cash-shifts/open',
    title: 'Kasa Vardiyası Aç (Shift Open)',
    category: 'retail',
    description: 'Açılış nakit tutarı ile yeni bir kasa satış vardiyası başlatır.',
    bodyFields: [
      { name: 'openingCash', label: 'Açılış Kasa Nakdi (₺)', type: 'number', required: true },
    ],
    generateMock: () => ({
      body: { openingCash: 1500 },
    }),
  },
  {
    id: 'pos-shift-close',
    method: 'POST',
    path: '/api/cash-shifts/{id}/close',
    title: 'Kasa Vardiyasını Kapat (Shift Close)',
    category: 'retail',
    description: 'Sayılan nakit tutarı ile vardiyayı kapatır ve kasa farkını hesaplar.',
    pathParams: [
      { name: 'id', label: 'Vardiya ID', type: 'text', required: true },
    ],
    bodyFields: [
      { name: 'countedCash', label: 'Sayılan Kasa Nakdi (₺)', type: 'number', required: true },
      { name: 'note', label: 'Kapanış Notu', type: 'text' },
    ],
    generateMock: () => ({
      path: { id: randId() },
      body: { countedCash: 8750, note: 'Günün sonu kasa mutabakatı yapıldı.' },
    }),
  },
  {
    id: 'pos-sales-list',
    method: 'GET',
    path: '/api/quick-sales',
    title: 'Hızlı Satışları Listele',
    category: 'retail',
    description: 'Kasa ekranından yapılan perakende satış işlemlerini listeler.',
    queryParams: [
      { name: 'limit', label: 'Limit', type: 'number' },
    ],
    generateMock: () => ({
      query: { limit: 20 },
    }),
  },

  // ==========================================================================
  // 6. INVENTORY & PRODUCTS (Ürünler & Stok)
  // ==========================================================================
  {
    id: 'prod-list',
    method: 'GET',
    path: '/api/products',
    title: 'Ürün ve Malzemeleri Listele',
    category: 'inventory',
    description: 'Sistemde kayıtlı yedek parça ve sarf malzemeleri listeler.',
    queryParams: [
      { name: 'search', label: 'Arama', type: 'text' },
      { name: 'activeOnly', label: 'Yalnızca Aktifler', type: 'boolean' },
    ],
    generateMock: () => ({
      query: { activeOnly: true },
    }),
  },
  {
    id: 'prod-create',
    method: 'POST',
    path: '/api/products',
    title: 'Yeni Ürün / Malzeme Kartı Aç',
    category: 'inventory',
    description: 'Stok takibi yapılan yeni bir malzeme kartı tanımlar.',
    bodyFields: [
      { name: 'code', label: 'Malzeme Kodu (SKU)', type: 'text', required: true },
      { name: 'name', label: 'Malzeme Adı', type: 'text', required: true },
      { name: 'barcode', label: 'Barkod', type: 'text' },
      { name: 'unit', label: 'Ölçü Birimi', type: 'text', required: true, helperText: 'Adet, Metre, Kg, Paket' },
      { name: 'salePrice', label: 'Satış Fiyatı (₺)', type: 'number', required: true },
      { name: 'vatRate', label: 'KDV Oranı (%)', type: 'number', required: true },
      { name: 'tracksStock', label: 'Stok Takibi Yapılsın', type: 'boolean' },
    ],
    generateMock: () => {
      const n = rand(100, 999)
      return {
        body: {
          code: `ELK-PRT-${n}`,
          name: `ABB Termik Manyetik Şalter ${n}A`,
          barcode: `869000112${n}`,
          unit: 'Adet',
          salePrice: rand(15, 80) * 100,
          vatRate: 20,
          tracksStock: true,
        },
      }
    },
  },
  {
    id: 'inv-warehouses',
    method: 'GET',
    path: '/api/inventory/warehouses',
    title: 'Depoları Listele',
    category: 'inventory',
    description: 'Merkez ve saha depolarını listeler.',
    generateMock: () => ({}),
  },
  {
    id: 'inv-movements',
    method: 'POST',
    path: '/api/inventory/movements',
    title: 'Stok Hareketi İşle (Giriş/Çıkış)',
    category: 'inventory',
    description: 'Depoya mal kabulü veya sahadan malzeme çıkış hareketi kaydeder.',
    bodyFields: [
      { name: 'productId', label: 'Malzeme ID', type: 'text', required: true },
      { name: 'warehouseId', label: 'Depo ID', type: 'text', required: true },
      {
        name: 'type',
        label: 'Hareket Türü',
        type: 'enum',
        required: true,
        options: [
          { value: 'IN', label: 'Giriş (Mal Kabul)', color: 'success' },
          { value: 'OUT', label: 'Çıkış (Servise Sarf)', color: 'error' },
          { value: 'NEUTRAL', label: 'Düzeltme / Sayım', color: 'info' },
        ],
      },
      { name: 'quantity', label: 'Miktar', type: 'number', required: true },
      { name: 'unitCost', label: 'Birim Maliyet (₺)', type: 'number', required: true },
      { name: 'notes', label: 'Hareket Açıklaması', type: 'text' },
    ],
    generateMock: () => ({
      body: {
        productId: randId(),
        warehouseId: randId(),
        type: 'IN',
        quantity: rand(10, 50),
        unitCost: rand(100, 500),
        notes: 'Haftalık tedarikçi sevkiyatı kabul edildi.',
      },
    }),
  },

  // ==========================================================================
  // 8. REMINDERS & NOTIFICATIONS (Hatırlatıcılar)
  // ==========================================================================
  {
    id: 'rem-list',
    method: 'GET',
    path: '/api/reminders',
    title: 'Hatırlatıcıları Listele',
    category: 'reminders',
    description: 'Vadesi gelen veya bekleyen operasyonel hatırlatıcıları listeler.',
    queryParams: [
      {
        name: 'state',
        label: 'Durum',
        type: 'enum',
        options: [
          { value: '', label: 'Tümü' },
          { value: 'Pending', label: 'Bekliyor (Pending)', color: 'warning' },
          { value: 'Completed', label: 'Tamamlandı (Completed)', color: 'success' },
          { value: 'Dismissed', label: 'Kapatıldı (Dismissed)', color: 'default' },
        ],
      },
    ],
    generateMock: () => ({
      query: { state: 'Pending' },
    }),
  },
  {
    id: 'rem-create',
    method: 'POST',
    path: '/api/reminders',
    title: 'Yeni Hatırlatıcı Kur',
    category: 'reminders',
    description: 'Müşteri veya iş emrine bağlı alarm / hatırlatma kaydı oluşturur.',
    bodyFields: [
      {
        name: 'type',
        label: 'Hatırlatıcı Türü',
        type: 'enum',
        required: true,
        options: [
          { value: 'PAYMENT_DUE', label: 'Ödeme Vadesi Alarmı', color: 'error' },
          { value: 'WORK_ORDER_VISIT', label: 'Saha Servis Ziyareti', color: 'info' },
          { value: 'QUOTE_EXPIRY', label: 'Teklif Son Gün Hatırlatması', color: 'warning' },
        ],
      },
      { name: 'entityName', label: 'İlişkili Varlık Türü', type: 'text', required: true, helperText: 'Customer / WorkOrder' },
      { name: 'entityId', label: 'Varlık ID (EntityId)', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'dueAt', label: 'Hatırlatma Zamanı', type: 'datetime', required: true },
      { name: 'message', label: 'Bildirim Mesajı', type: 'text', required: true },
    ],
    generateMock: (l) => ({
      body: {
        type: 'PAYMENT_DUE',
        entityName: 'Customer',
        entityId: l?.customerId || '572a1548-dbdc-4b71-9c60-a2ea03d6d03f',
        dueAt: '2026-10-05T09:00:00Z',
        message: 'Müşterinin 2. hakediş fatura ödeme vadesi dolmak üzere.',
      },
    }),
  },

  // ==========================================================================
  // 9. IDENTITY & AUTH (Kimlik)
  // ==========================================================================
  {
    id: 'auth-register',
    method: 'POST',
    path: '/api/auth/register',
    title: 'Yeni Kullanıcı Kaydı (Register)',
    category: 'identity',
    description: 'Sisteme yeni bir kullanıcı hesabı başvurusu gönderir.',
    bodyFields: [
      { name: 'name', label: 'Ad Soyad', type: 'text', required: true },
      { name: 'email', label: 'E-Posta Adresi', type: 'text', required: true },
      { name: 'password', label: 'Şifre', type: 'text', required: true },
      { name: 'otp', label: 'Doğrulama Kodu (OTP - Opsiyonel)', type: 'text' },
    ],
    generateMock: () => {
      const id = rand(100, 999)
      return {
        body: {
          name: `Ali Teknisyen (${id})`,
          email: `teknisyen${id}@voltflow-test.com`,
          password: 'TestPassword123!',
        },
      }
    },
  },
  {
    id: 'auth-login',
    method: 'POST',
    path: '/api/auth/login',
    title: 'Kullanıcı Girişi (Login)',
    category: 'identity',
    description: 'E-posta ve şifre ile JWT oturum belirteci alır.',
    bodyFields: [
      { name: 'email', label: 'E-Posta Adresi', type: 'text', required: true },
      { name: 'password', label: 'Şifre', type: 'text', required: true },
    ],
    generateMock: () => ({
      body: {
        email: 'admin@voltflow.staging',
        password: 'DrJ7fcoQfZlWk1jx15Oh',
      },
    }),
  },

  // ==========================================================================
  // 10. SYSTEM & OPERATIONS (Sistem & Denetim)
  // ==========================================================================
  {
    id: 'sys-audit-recent',
    method: 'GET',
    path: '/api/audit-logs/recent',
    title: 'Son Denetim Kayıtlarını Getir (Audit Logs)',
    category: 'system',
    description: 'Sistemde yapılan son kritik mutasyonların denetim izlerini listeler.',
    generateMock: () => ({}),
  },
  {
    id: 'sys-tables',
    method: 'GET',
    path: '/api/test-data/tables',
    title: 'Tüm Tablo İstatistiklerini Getir',
    category: 'system',
    description: 'PostgreSQL veritabanındaki 41 tablonun canlı satır sayılarını döner.',
    generateMock: () => ({}),
  },
]
