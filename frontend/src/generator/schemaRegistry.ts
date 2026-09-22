export type FieldType = 'text' | 'number' | 'boolean' | 'date' | 'datetime' | 'enum' | 'fk'

export interface FieldDefinition {
  name: string
  label: string
  type: FieldType
  required?: boolean
  options?: { value: string | number | boolean; label: string; color?: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' }[]
  fkEntity?: 'Customer' | 'User' | 'Warehouse' | 'Project' | 'Quote' | 'WorkOrder' | 'Site'
  helperText?: string
}

export interface TableDefinition {
  name: string
  displayName: string
  category: string
  description: string
  fields: FieldDefinition[]
  generateMock: (lookups?: { customerId?: string; userId?: string; warehouseId?: string; projectId?: string }) => Record<string, unknown>
}

export interface TableCategory {
  id: string
  label: string
  iconName: string
}

export const TABLE_CATEGORIES: TableCategory[] = [
  { id: 'all', label: 'Tüm Tablolar (41)', iconName: 'all' },
  { id: 'identity', label: 'Kimlik & Yetki (5)', iconName: 'identity' },
  { id: 'crm', label: 'Müşteri & CRM (3)', iconName: 'crm' },
  { id: 'quotes', label: 'Teklifler (2)', iconName: 'quotes' },
  { id: 'workorders', label: 'İş Emirleri (3)', iconName: 'workorders' },
  { id: 'inventory', label: 'Envanter & Stok (4)', iconName: 'inventory' },
  { id: 'finance', label: 'Finans & Muhasebe (6)', iconName: 'finance' },
  { id: 'retail', label: 'POS & Perakende (6)', iconName: 'retail' },
  { id: 'projects', label: 'Projeler (2)', iconName: 'projects' },
  { id: 'maintenance', label: 'Bakım & Bildirim (2)', iconName: 'maintenance' },
  { id: 'system', label: 'Sistem & Denetim (8)', iconName: 'system' },
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

export const ALL_TABLES: TableDefinition[] = [
  // --------------------------------------------------------------------------
  // 1. IDENTITY & AUTH (5)
  // --------------------------------------------------------------------------
  {
    name: 'AppUsers',
    displayName: 'Kullanıcı Hesapları (AppUsers)',
    category: 'identity',
    description: 'Sistem kullanıcıları, oturum kimlikleri ve şifreleri',
    fields: [
      { name: 'Name', label: 'Ad Soyad', type: 'text', required: true },
      { name: 'Email', label: 'E-Posta Adresi', type: 'text', required: true },
      { name: 'PasswordHash', label: 'Şifre', type: 'text', required: true, helperText: 'Düz metin girilebilir (otomatik hashlenir)' },
      { name: 'IsApproved', label: 'Hesap Onaylı', type: 'boolean' },
      { name: 'EmailVerified', label: 'E-Posta Doğrulandı', type: 'boolean' },
    ],
    generateMock: () => {
      const names = ['Kerem Güneş', 'Seda Aksoy', 'Murat Koç', 'Büşra Arslan', 'Ozan Çelik']
      const n = names[rand(0, names.length - 1)]
      const email = `${n.toLowerCase().replace(' ', '.')}${rand(10, 99)}@voltflow-test.com`
      return {
        Name: n,
        Email: email,
        PasswordHash: 'TestPassword123!',
        IsApproved: true,
        EmailVerified: true,
      }
    },
  },
  {
    name: 'AppRoles',
    displayName: 'Sistem Rolleri (AppRoles)',
    category: 'identity',
    description: 'Tanımlı kullanıcı yetki rolleri',
    fields: [
      { name: 'Name', label: 'Rol Adı', type: 'text', required: true },
    ],
    generateMock: () => ({
      Name: `CustomRole_${rand(100, 999)}`,
    }),
  },
  {
    name: 'AppUserRoles',
    displayName: 'Kullanıcı Rol Atamaları (AppUserRoles)',
    category: 'identity',
    description: 'Kullanıcı ile rol eşleştirmeleri',
    fields: [
      { name: 'UserId', label: 'Kullanıcı (User)', type: 'fk', fkEntity: 'User', required: true },
      {
        name: 'RoleId',
        label: 'Rol Seçimi',
        type: 'enum',
        required: true,
        options: [
          { value: 'd333aaa6-41e4-408d-99f2-e53f1cb4ec51', label: 'Admin', color: 'error' },
          { value: '82724aab-be96-4a7a-a3f5-b8d95459aa79', label: 'Manager', color: 'primary' },
          { value: '7681417b-7667-48ff-8258-aee3e7bcf8bc', label: 'Technician', color: 'warning' },
          { value: '54b8d458-2c5b-48f8-b404-161bf50c0e9d', label: 'Viewer', color: 'default' },
        ],
      },
    ],
    generateMock: (l) => ({
      UserId: l?.userId || randId(),
      RoleId: '7681417b-7667-48ff-8258-aee3e7bcf8bc',
    }),
  },
  {
    name: 'UserSessions',
    displayName: 'Kullanıcı Oturumları (UserSessions)',
    category: 'identity',
    description: 'Aktif ve geçmiş JWT/token oturum kayıtları',
    fields: [
      { name: 'UserId', label: 'Kullanıcı', type: 'fk', fkEntity: 'User', required: true },
      { name: 'TokenHash', label: 'Oturum Token Hash', type: 'text', required: true },
      { name: 'ExpiresAt', label: 'Bitiş Tarihi', type: 'datetime', required: true },
      { name: 'RevokedAt', label: 'İptal Tarihi', type: 'datetime' },
    ],
    generateMock: (l) => {
      const exp = new Date()
      exp.setDate(exp.getDate() + 7)
      return {
        UserId: l?.userId || randId(),
        TokenHash: `hash_${randId().substring(0, 16)}`,
        ExpiresAt: exp.toISOString(),
      }
    },
  },
  {
    name: 'PasswordResetTokens',
    displayName: 'Şifre Sıfırlama Tokenları (PasswordResetTokens)',
    category: 'identity',
    description: 'Kullanıcı şifre sıfırlama talepleri ve doğrulama kodları',
    fields: [
      { name: 'UserId', label: 'Kullanıcı', type: 'fk', fkEntity: 'User', required: true },
      { name: 'TokenHash', label: 'Reset Token Hash', type: 'text', required: true },
      { name: 'ExpiresAt', label: 'Geçerlilik Bitiş', type: 'datetime', required: true },
      { name: 'UsedAt', label: 'Kullanılma Tarihi', type: 'datetime' },
    ],
    generateMock: (l) => {
      const exp = new Date()
      exp.setHours(exp.getHours() + 2)
      return {
        UserId: l?.userId || randId(),
        TokenHash: `reset_${randId().substring(0, 12)}`,
        ExpiresAt: exp.toISOString(),
      }
    },
  },

  // --------------------------------------------------------------------------
  // 2. CRM & CUSTOMERS (3)
  // --------------------------------------------------------------------------
  {
    name: 'Customers',
    displayName: 'Müşteriler & Adaylar (Customers)',
    category: 'crm',
    description: 'Tüm ticari müşteriler ve potansiyel aday (Lead) kayıtları',
    fields: [
      { name: 'FullName', label: 'Müşteri Ünvanı / Adı', type: 'text', required: true },
      { name: 'Email', label: 'E-Posta', type: 'text', required: true },
      { name: 'Phone', label: 'Telefon', type: 'text', required: true },
      { name: 'TaxNumber', label: 'Vergi No / TCKN', type: 'text', required: true },
      {
        name: 'Type',
        label: 'Müşteri Tipi (Enum)',
        type: 'enum',
        options: [
          { value: 1, label: 'Active (Kazanılmış Müşteri)', color: 'success' },
          { value: 0, label: 'Lead (Aday Müşteri)', color: 'info' },
        ],
      },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => {
      const cos = ['Delta Endüstriyel', 'Marmara Çelik', 'Toros Tesisat', 'Batı Otomasyon', 'Anadolu Makine']
      const name = `${cos[rand(0, cos.length - 1)]} San. Tic. A.Ş.`
      return {
        FullName: name,
        Email: `info@${name.substring(0, 5).toLowerCase()}${rand(10, 99)}.com`,
        Phone: `+90532${rand(100, 999)}${rand(10, 99)}`,
        TaxNumber: `${rand(1000000000, 9999999999)}`,
        Type: rand(0, 1),
        IsActive: true,
      }
    },
  },
  {
    name: 'CustomerSites',
    displayName: 'Müşteri Şantiyeleri / Tesisleri (CustomerSites)',
    category: 'crm',
    description: 'Müşteriye bağlı lokasyon, fabrika, bina ve şantiyeler',
    fields: [
      { name: 'CustomerId', label: 'Müşteri Seçimi', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Name', label: 'Tesis / Şantiye Adı', type: 'text', required: true },
      { name: 'Address', label: 'Açık Adres', type: 'text', required: true },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: (l) => ({
      CustomerId: l?.customerId || randId(),
      Name: `Şantiye ${rand(1, 10)}. Etap`,
      Address: `Organize Sanayi Bölgesi 4. Cadde No:${rand(1, 50)} Tuzla / İstanbul`,
      IsActive: true,
    }),
  },
  {
    name: 'CustomerAssets',
    displayName: 'Müşteri Varlıkları & Cihazları (CustomerAssets)',
    category: 'crm',
    description: 'Tesislerde kurulu trafo, pano, jeneratör ve UPS cihazları',
    fields: [
      { name: 'SiteId', label: 'Tesis ID (SiteId)', type: 'text', required: true },
      { name: 'Name', label: 'Cihaz / Varlık Adı', type: 'text', required: true },
      { name: 'SerialNumber', label: 'Seri Numarası', type: 'text', required: true },
      { name: 'InstallationDate', label: 'Montaj / Kurulum Tarihi', type: 'date' },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => ({
      SiteId: randId(),
      Name: 'Schneider 1600kVA Kuru Tip Trafo Panosu',
      SerialNumber: `TRF-SN-${rand(10000, 99999)}`,
      InstallationDate: new Date().toISOString().split('T')[0],
      IsActive: true,
    }),
  },

  // --------------------------------------------------------------------------
  // 3. QUOTES (2)
  // --------------------------------------------------------------------------
  {
    name: 'Quotes',
    displayName: 'Teklifler (Quotes)',
    category: 'quotes',
    description: 'Müşteri fiyat teklifleri ve onay süreçleri (5 durum)',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Number', label: 'Teklif Numarası', type: 'text', required: true },
      { name: 'Title', label: 'Teklif Başlığı', type: 'text', required: true },
      { name: 'Total', label: 'Toplam Tutar (₺)', type: 'number', required: true },
      {
        name: 'State',
        label: 'Teklif Durumu (Enum)',
        type: 'enum',
        options: [
          { value: 0, label: 'Draft (Taslak)', color: 'default' },
          { value: 1, label: 'Issued (Gönderildi)', color: 'info' },
          { value: 2, label: 'Accepted (Onaylandı)', color: 'success' },
          { value: 3, label: 'Rejected (Reddedildi)', color: 'error' },
          { value: 4, label: 'Expired (Süresi Doldu)', color: 'warning' },
        ],
      },
      { name: 'RequiredDepositPercentage', label: 'Zorunlu Peşinat (%)', type: 'number' },
      { name: 'DepositPaidAmount', label: 'Ödenen Peşinat (₺)', type: 'number' },
      { name: 'RejectionReason', label: 'Reddetme Gerekçesi', type: 'text' },
      { name: 'IsChangeOrder', label: 'Revizyon / Ek Sipariş', type: 'boolean' },
    ],
    generateMock: (l) => {
      const titles = ['Fabrika Şalt Revizyonu', 'Yıllık Kompanzasyon Anlaşması', 'Yangın İhbar Sistemi Kurulumu']
      const st = rand(0, 4)
      return {
        CustomerId: l?.customerId || randId(),
        Number: `QT-2026-${rand(1000, 9999)}`,
        Title: titles[rand(0, titles.length - 1)],
        Total: rand(15, 95) * 1000,
        State: st,
        RequiredDepositPercentage: 30,
        DepositPaidAmount: st === 2 ? 15000 : 0,
        RejectionReason: st === 3 ? 'Bütçe kısıtlaması nedeniyle reddedildi.' : null,
        IsChangeOrder: false,
      }
    },
  },
  {
    name: 'QuoteItem',
    displayName: 'Teklif Kalemleri (QuoteItem)',
    category: 'quotes',
    description: 'Teklif içindeki satır kalemleri (Malzeme, İşçilik vb.)',
    fields: [
      { name: 'QuoteId', label: 'Teklif ID (QuoteId)', type: 'fk', fkEntity: 'Quote', required: true },
      { name: 'Description', label: 'Kalem Açıklaması', type: 'text', required: true },
      { name: 'Quantity', label: 'Miktar', type: 'number', required: true },
      { name: 'UnitPrice', label: 'Birim Fiyat (₺)', type: 'number', required: true },
      { name: 'Unit', label: 'Birim', type: 'text', required: true },
      { name: 'Kind', label: 'Kalem Türü (Material/Labor/Service)', type: 'text', required: true },
      { name: 'LineNumber', label: 'Satır No', type: 'number', required: true },
      { name: 'VatRate', label: 'KDV Oranı (%)', type: 'number', required: true },
    ],
    generateMock: () => ({
      QuoteId: randId(),
      Description: 'Siemens 3 Faz Kompakt Şalter 400A',
      Quantity: rand(1, 10),
      UnitPrice: rand(25, 80) * 100,
      Unit: 'Adet',
      Kind: 'Material',
      LineNumber: 1,
      VatRate: 20,
    }),
  },

  // --------------------------------------------------------------------------
  // 4. WORK ORDERS (3)
  // --------------------------------------------------------------------------
  {
    name: 'WorkOrders',
    displayName: 'İş Emirleri (WorkOrders)',
    category: 'workorders',
    description: 'Saha servis ve montaj iş emirleri (10 durum)',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Number', label: 'İş Emri Numarası', type: 'text', required: true },
      { name: 'Title', label: 'İş Emri Başlığı', type: 'text', required: true },
      {
        name: 'Status',
        label: 'İş Emri Durumu (Enum)',
        type: 'enum',
        options: [
          { value: 0, label: 'Open (Açık)', color: 'default' },
          { value: 1, label: 'Assigned (Atandı)', color: 'info' },
          { value: 2, label: 'EnRoute (Yolda)', color: 'secondary' },
          { value: 3, label: 'InProgress (Devam Ediyor)', color: 'warning' },
          { value: 4, label: 'OnHold (Beklemede)', color: 'error' },
          { value: 5, label: 'Completed (Tamamlandı)', color: 'success' },
          { value: 6, label: 'ReadyForBilling (Faturaya Hazır)', color: 'primary' },
          { value: 7, label: 'Invoiced (Faturalandı)', color: 'success' },
          { value: 8, label: 'Cancelled (İptal)', color: 'error' },
          { value: 9, label: 'NoShow (Adreste Yok)', color: 'error' },
        ],
      },
      { name: 'Total', label: 'Toplam Tutar (₺)', type: 'number', required: true },
      { name: 'AssignedUserId', label: 'Atanan Teknisyen', type: 'fk', fkEntity: 'User' },
      { name: 'IsSafetyChecklistCompleted', label: 'İSG Formu Tamamlandı', type: 'boolean' },
      { name: 'HoldReason', label: 'Bekleme Gerekçesi', type: 'text' },
      { name: 'CancellationReason', label: 'İptal / NoShow Nedeni', type: 'text' },
    ],
    generateMock: (l) => {
      const titles = ['Ana Dağıtım Panosu Kablolama', 'Termal Kamera Ölçümü', 'Jeneratör Akü Değişimi', 'UPS Akü Bakımı']
      const st = rand(0, 9)
      return {
        CustomerId: l?.customerId || randId(),
        Number: `WO-2026-${rand(1000, 9999)}`,
        Title: titles[rand(0, titles.length - 1)],
        Status: st,
        Total: rand(5, 50) * 1000,
        AssignedUserId: l?.userId || null,
        IsSafetyChecklistCompleted: st >= 3,
        HoldReason: st === 4 ? 'Şalt malzemesi bekleniyor' : null,
        CancellationReason: st === 8 ? 'Müşteri iptal etti' : st === 9 ? 'Kapı kapalıydı' : null,
      }
    },
  },
  {
    name: 'WorkOrderItem',
    displayName: 'İş Emri Malzemeleri (WorkOrderItem)',
    category: 'workorders',
    description: 'İş emrinde kullanılan sarf ve montaj malzemeleri',
    fields: [
      { name: 'WorkOrderId', label: 'İş Emri ID', type: 'fk', fkEntity: 'WorkOrder', required: true },
      { name: 'Description', label: 'Malzeme Açıklaması', type: 'text', required: true },
      { name: 'Quantity', label: 'Kullanılan Miktar', type: 'number', required: true },
      { name: 'UnitPrice', label: 'Birim Fiyat (₺)', type: 'number', required: true },
    ],
    generateMock: () => ({
      WorkOrderId: randId(),
      Description: '3x2.5 NYM Antigron Kablo',
      Quantity: rand(5, 50),
      UnitPrice: rand(15, 45) * 10,
    }),
  },
  {
    name: 'WorkOrderTimeEntry',
    displayName: 'İş Emri Mesai Kayıtları (WorkOrderTimeEntry)',
    category: 'workorders',
    description: 'Teknisyenlerin iş başında geçirdiği zaman ve notlar',
    fields: [
      { name: 'WorkOrderId', label: 'İş Emri ID', type: 'fk', fkEntity: 'WorkOrder', required: true },
      { name: 'CheckInTime', label: 'Giriş Saati (CheckIn)', type: 'datetime', required: true },
      { name: 'CheckOutTime', label: 'Çıkış Saati (CheckOut)', type: 'datetime' },
      { name: 'Notes', label: 'Çalışma Notları', type: 'text' },
    ],
    generateMock: () => {
      const now = new Date()
      const checkIn = new Date(now.getTime() - 4 * 3600 * 1000)
      return {
        WorkOrderId: randId(),
        CheckInTime: checkIn.toISOString(),
        CheckOutTime: now.toISOString(),
        Notes: 'Pano içi bara temizliği ve tork kontrolü yapıldı.',
      }
    },
  },

  // --------------------------------------------------------------------------
  // 5. INVENTORY & STOCKS (4)
  // --------------------------------------------------------------------------
  {
    name: 'Warehouses',
    displayName: 'Depolar & Araçlar (Warehouses)',
    category: 'inventory',
    description: 'Merkez depo, şantiye ve mobil servis araç depoları',
    fields: [
      { name: 'Name', label: 'Depo / Araç Adı', type: 'text', required: true },
      {
        name: 'Type',
        label: 'Depo Türü (Enum)',
        type: 'enum',
        options: [
          { value: 0, label: 'Main (Merkez Depo)', color: 'primary' },
          { value: 1, label: 'Van (Mobil Servis Aracı)', color: 'warning' },
          { value: 2, label: 'Virtual (Sanal Konsinye/İade)', color: 'default' },
        ],
      },
    ],
    generateMock: () => ({
      Name: `Avrupa Bölgesi Servis Aracı ${rand(1, 9)}`,
      Type: rand(0, 2),
    }),
  },
  {
    name: 'Products',
    displayName: 'Ürün & Malzeme Kataloğu (Products)',
    category: 'inventory',
    description: 'Fiziksel stok ürünleri ve saatlik işçilik kalemleri',
    fields: [
      { name: 'Code', label: 'Malzeme Kodu', type: 'text', required: true },
      { name: 'Name', label: 'Malzeme / Hizmet Adı', type: 'text', required: true },
      { name: 'Barcode', label: 'Barkod', type: 'text' },
      { name: 'Unit', label: 'Birim (Adet/Saat/Metre)', type: 'text', required: true },
      { name: 'SalePrice', label: 'Satış Fiyatı (₺)', type: 'number', required: true },
      { name: 'VatRate', label: 'KDV Oranı (%)', type: 'number', required: true },
      { name: 'TracksStock', label: 'Stok Takibi Yapılsın mı?', type: 'boolean' },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => {
      const items = ['Kablo Kanalı 100x50', 'Schneider 25A 3P Sigorta', 'LED Panel Armatür 60x60', 'Tork Anahtarı Kalibrasyon Servisi']
      const name = items[rand(0, items.length - 1)]
      return {
        Code: `PRD-${rand(100, 999)}`,
        Name: name,
        Barcode: `869${rand(100000000, 999999999)}`,
        Unit: 'Adet',
        SalePrice: rand(20, 200) * 10,
        VatRate: 20,
        TracksStock: true,
        IsActive: true,
      }
    },
  },
  {
    name: 'MaterialStocks',
    displayName: 'Depo Stok Miktarları (MaterialStocks)',
    category: 'inventory',
    description: 'Hangi depoda ne kadar malzeme var ve rezerve miktar',
    fields: [
      { name: 'WarehouseId', label: 'Depo Seçimi', type: 'fk', fkEntity: 'Warehouse', required: true },
      { name: 'MaterialCode', label: 'Malzeme Kodu', type: 'text', required: true },
      { name: 'Name', label: 'Malzeme Adı', type: 'text', required: true },
      { name: 'QuantityOnHand', label: 'Mevcut Stok Miktarı', type: 'number', required: true },
      { name: 'ReservedQuantity', label: 'Rezerve Edilen Miktar', type: 'number', required: true },
    ],
    generateMock: (l) => ({
      WarehouseId: l?.warehouseId || randId(),
      MaterialCode: `PRD-KBL-${rand(1, 9)}`,
      Name: '3x2.5 NYM Antigron Kablo',
      QuantityOnHand: rand(20, 150),
      ReservedQuantity: rand(0, 10),
    }),
  },
  {
    name: 'StockMovements',
    displayName: 'Stok Hareketleri (StockMovements)',
    category: 'inventory',
    description: 'Depo giriş, çıkış ve transfer hareket kayıtları',
    fields: [
      { name: 'MaterialCode', label: 'Malzeme Kodu', type: 'text', required: true },
      { name: 'QuantityDelta', label: 'Miktar Değişimi (+/-)', type: 'number', required: true },
      {
        name: 'Direction',
        label: 'Yön (Direction)',
        type: 'enum',
        options: [
          { value: 'IN', label: 'Giriş (IN)', color: 'success' },
          { value: 'OUT', label: 'Çıkış (OUT)', color: 'error' },
        ],
      },
      { name: 'NewQuantity', label: 'Yeni Miktar', type: 'number', required: true },
      { name: 'PreviousQuantity', label: 'Önceki Miktar', type: 'number', required: true },
      { name: 'Reason', label: 'Hareket Nedeni', type: 'text', required: true },
      { name: 'SourceWarehouseId', label: 'Kaynak Depo', type: 'fk', fkEntity: 'Warehouse' },
      { name: 'DestinationWarehouseId', label: 'Hedef Depo (Varsa)', type: 'fk', fkEntity: 'Warehouse' },
    ],
    generateMock: (l) => ({
      MaterialCode: 'PRD-KBL-01',
      QuantityDelta: -10,
      Direction: 'OUT',
      PreviousQuantity: 100,
      NewQuantity: 90,
      Reason: 'Saha Arıza Müdahalesi Çıkışı',
      SourceWarehouseId: l?.warehouseId || randId(),
    }),
  },

  // --------------------------------------------------------------------------
  // 6. FINANCE & INVOICES (6)
  // --------------------------------------------------------------------------
  {
    name: 'SalesInvoices',
    displayName: 'Satış Faturaları (SalesInvoices)',
    category: 'finance',
    description: 'Resmi satış faturaları, iade dekontları ve kalan bakiyeler',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'InvoiceNumber', label: 'Fatura Numarası', type: 'text', required: true },
      { name: 'GrandTotal', label: 'Fatura Genel Toplamı (₺)', type: 'number', required: true },
      { name: 'PaidAmount', label: 'Tahsil Edilen Tutar (₺)', type: 'number', required: true },
      { name: 'AppliedDepositAmount', label: 'Mahsup Peşinat (₺)', type: 'number' },
      { name: 'InvoiceDate', label: 'Fatura Tarihi', type: 'date', required: true },
      {
        name: 'Type',
        label: 'Fatura Türü (Enum)',
        type: 'enum',
        options: [
          { value: 'Standard', label: 'Standard (Normal Satış)', color: 'primary' },
          { value: 'CreditNote', label: 'CreditNote (İade/Alacak Dekontu)', color: 'warning' },
        ],
      },
    ],
    generateMock: (l) => {
      const tot = rand(10, 60) * 1000
      return {
        CustomerId: l?.customerId || randId(),
        InvoiceNumber: `INV-2026-${rand(1000, 9999)}`,
        GrandTotal: tot,
        PaidAmount: rand(0, 1) === 1 ? tot : 0,
        AppliedDepositAmount: 0,
        InvoiceDate: new Date().toISOString().split('T')[0],
        Type: 'Standard',
      }
    },
  },
  {
    name: 'CustomerPayments',
    displayName: 'Müşteri Tahsilatları (CustomerPayments)',
    category: 'finance',
    description: 'Müşterilerden alınan havale, kart ve nakit ödemeler',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Amount', label: 'Tahsilat Tutarı (₺)', type: 'number', required: true },
      {
        name: 'PaymentMethod',
        label: 'Ödeme Yöntemi (Enum)',
        type: 'enum',
        options: [
          { value: 'BANK_TRANSFER', label: 'Banka Havalesi / EFT', color: 'primary' },
          { value: 'CARD', label: 'Kredi / Banka Kartı', color: 'success' },
          { value: 'CASH', label: 'Nakit Kasa', color: 'warning' },
        ],
      },
      { name: 'PaymentDate', label: 'Tahsilat Tarihi', type: 'date', required: true },
      { name: 'AllocatedAmount', label: 'Faturalara Mahsup Edilen', type: 'number', required: true },
    ],
    generateMock: (l) => {
      const amt = rand(5, 40) * 1000
      return {
        CustomerId: l?.customerId || randId(),
        Amount: amt,
        PaymentMethod: 'BANK_TRANSFER',
        PaymentDate: new Date().toISOString().split('T')[0],
        AllocatedAmount: amt,
      }
    },
  },
  {
    name: 'PaymentInvoiceAllocations',
    displayName: 'Tahsilat Fatura Mahsupları (PaymentInvoiceAllocations)',
    category: 'finance',
    description: 'Hangi tahsilatın hangi faturayı kapattığı bilgisi',
    fields: [
      { name: 'PaymentId', label: 'Tahsilat ID (PaymentId)', type: 'text', required: true },
      { name: 'InvoiceId', label: 'Fatura ID (InvoiceId)', type: 'text', required: true },
      { name: 'Amount', label: 'Mahsup Tutarı (₺)', type: 'number', required: true },
    ],
    generateMock: () => ({
      PaymentId: randId(),
      InvoiceId: randId(),
      Amount: rand(5, 20) * 1000,
    }),
  },
  {
    name: 'CustomerLedgerEntries',
    displayName: 'Cari Hesap Hareketleri (CustomerLedgerEntries)',
    category: 'finance',
    description: 'Müşteri borç/alacak ekstresi ve bakiye değişimi',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Amount', label: 'Tutar (₺)', type: 'number', required: true },
      {
        name: 'Direction',
        label: 'Yön (DEBIT: Borç, CREDIT: Alacak)',
        type: 'enum',
        options: [
          { value: 'DEBIT', label: 'DEBIT (Müşteri Borçlandı)', color: 'error' },
          { value: 'CREDIT', label: 'CREDIT (Müşteri Ödedi)', color: 'success' },
        ],
      },
      { name: 'BalanceAfter', label: 'İşlem Sonrası Bakiye (₺)', type: 'number', required: true },
      { name: 'Description', label: 'Açıklama', type: 'text', required: true },
    ],
    generateMock: (l) => ({
      CustomerId: l?.customerId || randId(),
      Amount: rand(10, 30) * 1000,
      Direction: 'DEBIT',
      BalanceAfter: rand(20, 80) * 1000,
      Description: 'Satış Faturası Tahakkuku',
    }),
  },
  {
    name: 'BillingEntries',
    displayName: 'Hakediş & Faturalama Girişleri (BillingEntries)',
    category: 'finance',
    description: 'Proje hakediş faturalama kayıtları',
    fields: [
      { name: 'ProjectId', label: 'Proje', type: 'fk', fkEntity: 'Project', required: true },
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Amount', label: 'Faturalanan Tutar (₺)', type: 'number', required: true },
    ],
    generateMock: (l) => ({
      ProjectId: l?.projectId || randId(),
      CustomerId: l?.customerId || randId(),
      Amount: rand(25, 100) * 1000,
    }),
  },
  {
    name: 'ProgressBillings',
    displayName: 'Proje Hakediş Belgeleri (ProgressBillings)',
    category: 'finance',
    description: 'Müteahhitlik/taahhüt onaylı ve bekleyen hakedişleri',
    fields: [
      { name: 'ProjectId', label: 'Proje', type: 'fk', fkEntity: 'Project', required: true },
      { name: 'BillingNumber', label: 'Hakediş No', type: 'text', required: true },
      { name: 'RequestedAmount', label: 'Talep Edilen Tutar (₺)', type: 'number', required: true },
      { name: 'ApprovedAmount', label: 'Onaylanan Tutar (₺)', type: 'number', required: true },
      { name: 'DeductionAmount', label: 'Kesinti Tutarı (₺)', type: 'number', required: true },
      { name: 'IsApproved', label: 'Onaylandı mı?', type: 'boolean' },
    ],
    generateMock: (l) => {
      const req = rand(50, 200) * 1000
      const ok = rand(0, 1) === 1
      return {
        ProjectId: l?.projectId || randId(),
        BillingNumber: `PB-2026-${rand(10, 99)}`,
        RequestedAmount: req,
        ApprovedAmount: ok ? req - 5000 : 0,
        DeductionAmount: ok ? 5000 : 0,
        IsApproved: ok,
      }
    },
  },

  // --------------------------------------------------------------------------
  // 7. RETAIL & POS (6)
  // --------------------------------------------------------------------------
  {
    name: 'CashShifts',
    displayName: 'Kasa Vardiyaları (CashShifts)',
    category: 'retail',
    description: 'Kasiyer vardiya açılış ve kapanış kasaları',
    fields: [
      { name: 'CashierUserId', label: 'Kasiyer', type: 'fk', fkEntity: 'User', required: true },
      { name: 'CashierName', label: 'Kasiyer Adı', type: 'text', required: true },
      { name: 'OpeningCash', label: 'Açılış Kasası (₺)', type: 'number', required: true },
      {
        name: 'Status',
        label: 'Vardiya Durumu',
        type: 'enum',
        options: [
          { value: 'Open', label: 'Open (Açık Kasa)', color: 'warning' },
          { value: 'Closed', label: 'Closed (Kapanmış)', color: 'success' },
        ],
      },
      { name: 'OpenedAt', label: 'Açılış Zamanı', type: 'datetime', required: true },
      { name: 'ClosedAt', label: 'Kapanış Zamanı', type: 'datetime' },
      { name: 'CountedCash', label: 'Sayılan Kasa', type: 'number' },
      { name: 'ExpectedCash', label: 'Beklenen Kasa', type: 'number' },
    ],
    generateMock: (l) => ({
      CashierUserId: l?.userId || randId(),
      CashierName: 'Kasa Görevlisi',
      OpeningCash: 500,
      Status: 'Closed',
      OpenedAt: new Date(Date.now() - 8 * 3600 * 1000).toISOString(),
      ClosedAt: new Date().toISOString(),
      CountedCash: 3500,
      ExpectedCash: 3500,
    }),
  },
  {
    name: 'QuickSales',
    displayName: 'Hızlı Satışlar / Fişler (QuickSales)',
    category: 'retail',
    description: 'Tezgah ve perakende satış fişleri',
    fields: [
      { name: 'SaleNumber', label: 'Fiş / Satış No', type: 'text', required: true },
      { name: 'SoldAt', label: 'Satış Zamanı', type: 'datetime', required: true },
      { name: 'CashierUserId', label: 'Kasiyer', type: 'fk', fkEntity: 'User', required: true },
      { name: 'CashierName', label: 'Kasiyer Adı', type: 'text', required: true },
      { name: 'ShiftId', label: 'Vardiya ID', type: 'text', required: true },
      { name: 'GrandTotal', label: 'Toplam Tutar (₺)', type: 'number', required: true },
      { name: 'Subtotal', label: 'Ara Toplam (₺)', type: 'number', required: true },
      { name: 'VatTotal', label: 'KDV Tutarı (₺)', type: 'number', required: true },
      {
        name: 'Status',
        label: 'Durum',
        type: 'enum',
        options: [
          { value: 'Completed', label: 'Completed (Tamamlandı)', color: 'success' },
          { value: 'Voided', label: 'Voided (İptal Edildi)', color: 'error' },
        ],
      },
      { name: 'VoidReason', label: 'İptal Nedeni', type: 'text' },
    ],
    generateMock: (l) => ({
      SaleNumber: `POS-2026-${rand(1000, 9999)}`,
      SoldAt: new Date().toISOString(),
      CashierUserId: l?.userId || randId(),
      CashierName: 'murat',
      ShiftId: randId(),
      Subtotal: 1000,
      VatTotal: 200,
      GrandTotal: 1200,
      Status: 'Completed',
    }),
  },
  {
    name: 'QuickSaleLines',
    displayName: 'Hızlı Satış Satırları (QuickSaleLines)',
    category: 'retail',
    description: 'Hızlı satıştaki ürün satırları ve adetleri',
    fields: [
      { name: 'QuickSaleId', label: 'Satış ID (QuickSaleId)', type: 'text', required: true },
      { name: 'Description', label: 'Ürün Açıklaması', type: 'text', required: true },
      { name: 'Quantity', label: 'Satılan Adet', type: 'number', required: true },
      { name: 'UnitPrice', label: 'Birim Fiyat (₺)', type: 'number', required: true },
      { name: 'LineTotal', label: 'Satır Toplamı (₺)', type: 'number', required: true },
      { name: 'VatRate', label: 'KDV Oranı (%)', type: 'number', required: true },
    ],
    generateMock: () => ({
      QuickSaleId: randId(),
      Description: '16A Sigorta B Tipi',
      Quantity: 2,
      UnitPrice: 185,
      LineTotal: 370,
      VatRate: 20,
    }),
  },
  {
    name: 'QuickSalePayments',
    displayName: 'Hızlı Satış Ödemeleri (QuickSalePayments)',
    category: 'retail',
    description: 'Fişin nakit/kart ödeme kırılımı',
    fields: [
      { name: 'QuickSaleId', label: 'Satış ID', type: 'text', required: true },
      { name: 'PaymentMethod', label: 'Ödeme Yöntemi (CASH/CARD)', type: 'text', required: true },
      { name: 'Amount', label: 'Tutar (₺)', type: 'number', required: true },
    ],
    generateMock: () => ({
      QuickSaleId: randId(),
      PaymentMethod: 'CARD',
      Amount: 444,
    }),
  },
  {
    name: 'QuickSaleReturns',
    displayName: 'Satış İadeleri (QuickSaleReturns)',
    category: 'retail',
    description: 'Müşterinin geri getirdiği ürünlerin iade fişleri',
    fields: [
      { name: 'ReturnNumber', label: 'İade Fiş No', type: 'text', required: true },
      { name: 'QuickSaleId', label: 'İlgili Satış ID', type: 'text', required: true },
      { name: 'SaleNumber', label: 'Orijinal Fiş No', type: 'text', required: true },
      { name: 'ReturnedAt', label: 'İade Zamanı', type: 'datetime', required: true },
      { name: 'RefundTotal', label: 'İade Tutarı (₺)', type: 'number', required: true },
      { name: 'Reason', label: 'İade Gerekçesi', type: 'text', required: true },
      { name: 'RefundMethod', label: 'İade Ödeme Yöntemi', type: 'text', required: true },
    ],
    generateMock: () => ({
      ReturnNumber: `RET-2026-${rand(100, 999)}`,
      QuickSaleId: randId(),
      SaleNumber: 'POS-2026-0001',
      ReturnedAt: new Date().toISOString(),
      RefundTotal: 222,
      Reason: 'Müşteri projeden artan malzemeyi iade etti',
      RefundMethod: 'CASH',
    }),
  },
  {
    name: 'QuickSaleReturnLines',
    displayName: 'Satış İade Satırları (QuickSaleReturnLines)',
    category: 'retail',
    description: 'İade edilen kalemlerin detay satırları',
    fields: [
      { name: 'QuickSaleReturnId', label: 'İade ID', type: 'text', required: true },
      { name: 'QuickSaleLineId', label: 'Orijinal Satır ID', type: 'text', required: true },
      { name: 'Description', label: 'İade Ürün Açıklaması', type: 'text', required: true },
      { name: 'Quantity', label: 'İade Edilen Miktar', type: 'number', required: true },
      { name: 'RefundAmount', label: 'İade Edilen Tutar (₺)', type: 'number', required: true },
    ],
    generateMock: () => ({
      QuickSaleReturnId: randId(),
      QuickSaleLineId: randId(),
      Description: '16A Sigorta',
      Quantity: 1,
      RefundAmount: 222,
    }),
  },

  // --------------------------------------------------------------------------
  // 8. PROJECTS & CONTRACTING (2)
  // --------------------------------------------------------------------------
  {
    name: 'Projects',
    displayName: 'Taahhüt Projeleri (Projects)',
    category: 'projects',
    description: 'Büyük ölçekli şantiye ve elektrik taahhüt projeleri',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Number', label: 'Proje Numarası', type: 'text', required: true },
      { name: 'Name', label: 'Proje Adı', type: 'text', required: true },
      { name: 'Budget', label: 'Toplam Bütçe (₺)', type: 'number', required: true },
    ],
    generateMock: (l) => {
      const prjs = ['Plaza Akıllı Bina Altyapısı', 'GES Çatı Güneş Santrali', 'Fabrika Yüksek Gerilim Hattı']
      return {
        CustomerId: l?.customerId || randId(),
        Number: `PR-2026-${rand(10, 99)}`,
        Name: prjs[rand(0, prjs.length - 1)],
        Budget: rand(300, 1500) * 1000,
      }
    },
  },
  {
    name: 'ProjectPhase',
    displayName: 'Proje Fazları / Aşamaları (ProjectPhase)',
    category: 'projects',
    description: 'Projenin aşamaları (Faz 1, Faz 2 vb.)',
    fields: [
      { name: 'ProjectId', label: 'Proje ID', type: 'fk', fkEntity: 'Project', required: true },
      { name: 'Title', label: 'Aşama / Faz Başlığı', type: 'text', required: true },
      { name: 'PlannedAmount', label: 'Planlanan Bütçe (₺)', type: 'number', required: true },
    ],
    generateMock: (l) => ({
      ProjectId: l?.projectId || randId(),
      Title: `Faz ${rand(1, 4)} - Şalt ve Dağıtım Montajı`,
      PlannedAmount: rand(100, 300) * 1000,
    }),
  },

  // --------------------------------------------------------------------------
  // 9. MAINTENANCE & REMINDERS (2)
  // --------------------------------------------------------------------------
  {
    name: 'MaintenanceContracts',
    displayName: 'Bakım Sözleşmeleri (MaintenanceContracts)',
    category: 'maintenance',
    description: 'Yıllık ve periyodik trafo/pano bakım anlaşmaları',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Title', label: 'Sözleşme Başlığı', type: 'text', required: true },
      {
        name: 'FrequencyMonths',
        label: 'Periyot (Ay)',
        type: 'enum',
        options: [
          { value: 1, label: '1 Aylık Periyot' },
          { value: 3, label: '3 Aylık (Çeyrek Dönem)' },
          { value: 6, label: '6 Aylık (Yarıyıl)' },
          { value: 12, label: '12 Aylık (Yıllık Sözleşme)' },
        ],
      },
      { name: 'NextMaintenanceDate', label: 'Sıradaki Bakım Tarihi', type: 'datetime' },
      { name: 'IsActive', label: 'Sözleşme Aktif mi?', type: 'boolean' },
    ],
    generateMock: (l) => {
      const nextDate = new Date()
      nextDate.setMonth(nextDate.getMonth() + 3)
      return {
        CustomerId: l?.customerId || randId(),
        Title: 'Endüstriyel Tesis Yıllık Periyodik Bakım Sözleşmesi',
        FrequencyMonths: 6,
        NextMaintenanceDate: nextDate.toISOString(),
        IsActive: true,
      }
    },
  },
  {
    name: 'ReminderRecords',
    displayName: 'Hatırlatıcı Kayıtları (ReminderRecords)',
    category: 'maintenance',
    description: 'Teklif geçerlilik ve bakım zamanı otomatik bildirimleri',
    fields: [
      { name: 'Type', label: 'Hatırlatıcı Türü', type: 'text', required: true },
      { name: 'EntityName', label: 'İlgili Varlık Türü (Quote/Customer/WorkOrder)', type: 'text', required: true },
      { name: 'EntityId', label: 'Varlık ID', type: 'text', required: true },
      { name: 'DueAt', label: 'Vade / Hatırlatma Zamanı', type: 'datetime', required: true },
      { name: 'Message', label: 'Bildirim Mesajı', type: 'text', required: true },
      {
        name: 'State',
        label: 'Durum (Enum)',
        type: 'enum',
        options: [
          { value: 'Pending', label: 'Pending (Beklemede)', color: 'warning' },
          { value: 'Completed', label: 'Completed (İletildi)', color: 'success' },
          { value: 'Dismissed', label: 'Dismissed (İptal)', color: 'default' },
        ],
      },
      { name: 'Attempts', label: 'Deneme Sayısı', type: 'number' },
    ],
    generateMock: () => {
      const due = new Date()
      due.setDate(due.getDate() + 3)
      return {
        Type: 'QUOTE_EXPIRY',
        EntityName: 'Quote',
        EntityId: randId(),
        DueAt: due.toISOString(),
        Message: 'Teklif geçerlilik süresi 3 gün sonra dolacak.',
        State: 'Pending',
        Attempts: 0,
      }
    },
  },

  // --------------------------------------------------------------------------
  // 10. SYSTEM & AUDIT (8)
  // --------------------------------------------------------------------------
  {
    name: 'AuditLogs',
    displayName: 'Denetim Kayıtları (AuditLogs)',
    category: 'system',
    description: 'Kullanıcıların yaptığı işleme dair denetim günlüğü',
    fields: [
      { name: 'UserId', label: 'İşlemi Yapan Kullanıcı ID', type: 'fk', fkEntity: 'User', required: true },
      { name: 'Action', label: 'İşlem Adı', type: 'text', required: true },
      { name: 'EntityName', label: 'Varlık Adı (Tablo)', type: 'text', required: true },
      { name: 'EntityId', label: 'Etkilenen Varlık ID', type: 'text', required: true },
      { name: 'Details', label: 'İşlem Detayları (JSON veya Metin)', type: 'text', required: true },
      { name: 'Timestamp', label: 'İşlem Zamanı', type: 'datetime', required: true },
    ],
    generateMock: (l) => ({
      UserId: l?.userId || randId(),
      Action: 'APPROVE_WORK_ORDER',
      EntityName: 'WorkOrder',
      EntityId: randId(),
      Details: 'İş emri tamamlanarak muhasebeye iletildi.',
      Timestamp: new Date().toISOString(),
    }),
  },
  {
    name: 'AuditEvents',
    displayName: 'Denetim Olayları (AuditEvents)',
    category: 'system',
    description: 'Sistem içi domain event ve durum değişim günlükleri',
    fields: [
      { name: 'EventType', label: 'Olay Türü (EventType)', type: 'text', required: true },
      { name: 'EntityName', label: 'Varlık Türü', type: 'text', required: true },
      { name: 'EntityId', label: 'Varlık ID', type: 'text', required: true },
      { name: 'OperationId', label: 'İşlem ID (OperationId)', type: 'text', required: true },
      { name: 'SourceEndpoint', label: 'Tetikleyen Uç Nokta', type: 'text', required: true },
      { name: 'ChangedFieldsJson', label: 'Değişen Alanlar JSON', type: 'text', required: true },
      { name: 'BeforeJson', label: 'Önceki Durum JSON', type: 'text', required: true },
      { name: 'AfterJson', label: 'Sonraki Durum JSON', type: 'text', required: true },
    ],
    generateMock: () => ({
      EventType: 'WorkOrderStatusChanged',
      EntityName: 'WorkOrder',
      EntityId: randId(),
      OperationId: randId(),
      SourceEndpoint: '/api/workorders/complete',
      ChangedFieldsJson: '{"Status": "Completed"}',
      BeforeJson: '{"Status": "InProgress"}',
      AfterJson: '{"Status": "Completed"}',
    }),
  },
  {
    name: 'CommandRecords',
    displayName: 'Komut Kayıtları / WAL (CommandRecords)',
    category: 'system',
    description: 'Yazma öncesi niyet günlüğü (WAL: Write-Ahead Log) kayıtları',
    fields: [
      { name: 'CommandId', label: 'Komut ID (CommandId)', type: 'text', required: true },
      { name: 'CommandType', label: 'Komut Türü', type: 'text', required: true },
      { name: 'TriggerSource', label: 'Tetikleyen Kaynak (UI/API/Event)', type: 'text', required: true },
      { name: 'PayloadJson', label: 'Girdi Verisi (Payload JSON)', type: 'text', required: true },
      {
        name: 'Status',
        label: 'Komut Durumu',
        type: 'enum',
        options: [
          { value: 'Pending', label: 'Pending (Bekliyor)', color: 'warning' },
          { value: 'Completed', label: 'Completed (Başarılı)', color: 'success' },
          { value: 'Failed', label: 'Failed (Hata Aldı)', color: 'error' },
        ],
      },
      { name: 'ErrorCode', label: 'Hata Kodu (Varsa)', type: 'text' },
    ],
    generateMock: () => ({
      CommandId: randId(),
      CommandType: 'CreateWorkOrderCommand',
      TriggerSource: 'UI',
      PayloadJson: '{"title": "Test Work Order"}',
      Status: 'Completed',
    }),
  },
  {
    name: 'ExecutionGuards',
    displayName: 'İdempotency Korumaları (ExecutionGuards)',
    category: 'system',
    description: 'Aynı komutun mükerrer çalışmasını engelleyen koruma kayıtları',
    fields: [
      { name: 'IdempotencyKey', label: 'İdempotency Anahtarı', type: 'text', required: true },
      { name: 'Scope', label: 'Kapsam (Scope)', type: 'text', required: true },
      { name: 'RequestHash', label: 'İstek Özeti (RequestHash)', type: 'text', required: true },
      {
        name: 'State',
        label: 'Durum',
        type: 'enum',
        options: [
          { value: 'Acquired', label: 'Acquired (Kilitlendi)', color: 'warning' },
          { value: 'Completed', label: 'Completed (Bitti)', color: 'success' },
        ],
      },
      { name: 'ResponseStatusCode', label: 'HTTP Yanıt Kodu', type: 'number', required: true },
      { name: 'ExpiresAt', label: 'Kilit Bitiş Zamanı', type: 'datetime', required: true },
    ],
    generateMock: () => {
      const exp = new Date()
      exp.setMinutes(exp.getMinutes() + 15)
      return {
        IdempotencyKey: randId(),
        Scope: 'CreateQuote',
        RequestHash: 'sha256_mock_hash_value',
        State: 'Completed',
        ResponseStatusCode: 200,
        ExpiresAt: exp.toISOString(),
      }
    },
  },
  {
    name: 'OperationTraces',
    displayName: 'İşlem İzleri & Telemetri (OperationTraces)',
    category: 'system',
    description: 'API istek süreleri, uç noktalar ve W3C trace telemetrisi',
    fields: [
      { name: 'OperationId', label: 'İşlem ID', type: 'text', required: true },
      { name: 'Endpoint', label: 'İstek Yapılan Uç Nokta', type: 'text', required: true },
      { name: 'StatusCode', label: 'HTTP Durum Kodu', type: 'number', required: true },
      { name: 'DurationMilliseconds', label: 'Süre (Milisaniye)', type: 'number', required: true },
      { name: 'Outcome', label: 'Sonuç (Success / Error)', type: 'text', required: true },
      { name: 'CompletedAt', label: 'Bitiş Zamanı', type: 'datetime', required: true },
      { name: 'ErrorMessage', label: 'Hata Mesajı (Varsa)', type: 'text' },
    ],
    generateMock: () => ({
      OperationId: randId(),
      Endpoint: '/api/quotes',
      StatusCode: 200,
      DurationMilliseconds: rand(15, 85),
      Outcome: 'Success',
      CompletedAt: new Date().toISOString(),
    }),
  },
  {
    name: 'OutboxMessages',
    displayName: 'Outbox Kuyruk Mesajları (OutboxMessages)',
    category: 'system',
    description: 'Asenkron mesajlaşma ve event publishing kuyruğu',
    fields: [
      { name: 'EventType', label: 'Olay / Mesaj Türü', type: 'text', required: true },
      { name: 'PayloadJson', label: 'Mesaj Yükü (JSON)', type: 'text', required: true },
      { name: 'OccurredAt', label: 'Oluşma Zamanı', type: 'datetime', required: true },
      {
        name: 'State',
        label: 'Kuyruk Durumu',
        type: 'enum',
        options: [
          { value: 'Pending', label: 'Pending (Gönderim Bekliyor)', color: 'warning' },
          { value: 'Processed', label: 'Processed (İşlendi)', color: 'success' },
          { value: 'DeadLetter', label: 'DeadLetter (Hatalı / DLQ)', color: 'error' },
        ],
      },
      { name: 'Attempts', label: 'Deneme Sayısı', type: 'number', required: true },
      { name: 'LastError', label: 'Son Hata Detayı', type: 'text' },
    ],
    generateMock: () => ({
      EventType: 'Voltflow.Domain.WorkOrders.WorkOrderCreatedEvent',
      PayloadJson: '{"WorkOrderId": "mock-uuid", "Number": "WO-2026-001"}',
      OccurredAt: new Date().toISOString(),
      State: 'Processed',
      Attempts: 1,
    }),
  },
  {
    name: 'ReferenceValues',
    displayName: 'Referans Değerler (ReferenceValues)',
    category: 'system',
    description: 'Sistem parametreleri, vergi dilimleri ve katsayılar',
    fields: [
      { name: 'Key', label: 'Anahtar (Key)', type: 'text', required: true },
      { name: 'Value', label: 'Değer (Value)', type: 'text', required: true },
      { name: 'Category', label: 'Kategori', type: 'text', required: true },
    ],
    generateMock: () => ({
      Key: `PARAM_SETTING_${rand(10, 99)}`,
      Value: `${rand(10, 500)}`,
      Category: 'SystemConfig',
    }),
  },
  {
    name: 'DocumentCounters',
    displayName: 'Numara Sayaçları (DocumentCounters)',
    category: 'system',
    description: 'İş emri, teklif ve fatura ardışık numara üreteçleri',
    fields: [
      { name: 'Key', label: 'Sayaç Anahtarı (Örn: WO-2026)', type: 'text', required: true },
      { name: 'LastValue', label: 'Son Üretilen Numara Değeri', type: 'number', required: true },
    ],
    generateMock: () => ({
      Key: `COUNTER_CUSTOM_${rand(10, 99)}`,
      LastValue: rand(100, 999),
    }),
  },
]
