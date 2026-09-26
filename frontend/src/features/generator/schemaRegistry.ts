export type FieldType = 'text' | 'number' | 'boolean' | 'date' | 'datetime' | 'enum' | 'fk'

export interface FieldDefinition {
  name: string
  label: string
  type: FieldType
  required?: boolean
  options?: { value: string | number | boolean; label: string; color?: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' }[]
  fkEntity?: 'Customer' | 'User' | 'Warehouse' | 'Service' | 'Site'
  helperText?: string
}

export interface TableDefinition {
  name: string
  displayName: string
  category: string
  description: string
  fields: FieldDefinition[]
  generateMock: (lookups?: { customerId?: string; userId?: string; warehouseId?: string; serviceId?: string }) => Record<string, unknown>
}

export interface TableCategory {
  id: string
  label: string
  iconName: string
}

export const TABLE_CATEGORIES: TableCategory[] = [
  { id: 'all', label: 'Tüm Tablolar (28)', iconName: 'all' },
  { id: 'identity', label: 'Kimlik & Yetki (5)', iconName: 'identity' },
  { id: 'crm', label: 'Müşteri & CRM (3)', iconName: 'crm' },
  { id: 'services', label: 'Hizmetler (2)', iconName: 'services' },
  { id: 'inventory', label: 'Envanter & Stok (4)', iconName: 'inventory' },
  { id: 'finance', label: 'Finans & Muhasebe (5)', iconName: 'finance' },
  { id: 'system', label: 'Sistem & Denetim (9)', iconName: 'system' },
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
  // 1. IDENTITY
  {
    name: 'AppUsers', displayName: 'Kullanıcı Hesapları (AppUsers)', category: 'identity', description: 'Sistem kullanıcıları',
    fields: [
      { name: 'Name', label: 'Ad Soyad', type: 'text', required: true },
      { name: 'Email', label: 'E-Posta Adresi', type: 'text', required: true },
      { name: 'PasswordHash', label: 'Şifre', type: 'text', required: true, helperText: 'Düz metin girilebilir (otomatik hashlenir)' },
      { name: 'IsApproved', label: 'Hesap Onaylı', type: 'boolean' },
      { name: 'EmailVerified', label: 'E-Posta Doğrulandı', type: 'boolean' },
    ],
    generateMock: () => {
      const names = ['Kerem Güneş', 'Seda Aksoy', 'Murat Koç']
      const n = names[rand(0, names.length - 1)]
      return { Name: n, Email: `${n.toLowerCase().replace(' ', '.')}${rand(10, 99)}@test.com`, PasswordHash: 'Test1234!', IsApproved: true, EmailVerified: true }
    },
  },
  {
    name: 'AppRoles', displayName: 'Sistem Rolleri (AppRoles)', category: 'identity', description: 'Tanımlı kullanıcı yetki rolleri',
    fields: [{ name: 'Name', label: 'Rol Adı', type: 'text', required: true }],
    generateMock: () => ({ Name: `CustomRole_${rand(100, 999)}` }),
  },
  {
    name: 'AppUserRoles', displayName: 'Kullanıcı Rol Atamaları (AppUserRoles)', category: 'identity', description: 'Kullanıcı rol eşleştirmeleri',
    fields: [
      { name: 'UserId', label: 'Kullanıcı', type: 'fk', fkEntity: 'User', required: true },
      { name: 'RoleId', label: 'Rol ID', type: 'text', required: true }
    ],
    generateMock: (l) => ({ UserId: l?.userId || randId(), RoleId: randId() }),
  },
  {
    name: 'UserSessions', displayName: 'Kullanıcı Oturumları (UserSessions)', category: 'identity', description: 'Aktif ve geçmiş JWT/token oturum kayıtları',
    fields: [
      { name: 'UserId', label: 'Kullanıcı', type: 'fk', fkEntity: 'User', required: true },
      { name: 'TokenHash', label: 'Oturum Token Hash', type: 'text', required: true },
      { name: 'ExpiresAt', label: 'Bitiş Tarihi', type: 'datetime', required: true },
      { name: 'RevokedAt', label: 'İptal Tarihi', type: 'datetime' },
    ],
    generateMock: (l) => {
      const exp = new Date(); exp.setDate(exp.getDate() + 7)
      return { UserId: l?.userId || randId(), TokenHash: `hash_${randId().substring(0, 16)}`, ExpiresAt: exp.toISOString() }
    },
  },
  {
    name: 'PasswordResetTokens', displayName: 'Şifre Sıfırlama Tokenları', category: 'identity', description: 'Kullanıcı şifre sıfırlama talepleri',
    fields: [
      { name: 'UserId', label: 'Kullanıcı', type: 'fk', fkEntity: 'User', required: true },
      { name: 'TokenHash', label: 'Reset Token Hash', type: 'text', required: true },
      { name: 'ExpiresAt', label: 'Geçerlilik Bitiş', type: 'datetime', required: true },
      { name: 'UsedAt', label: 'Kullanılma Tarihi', type: 'datetime' },
    ],
    generateMock: (l) => {
      const exp = new Date(); exp.setHours(exp.getHours() + 2)
      return { UserId: l?.userId || randId(), TokenHash: `reset_${randId().substring(0, 12)}`, ExpiresAt: exp.toISOString() }
    },
  },

  // 2. CRM
  {
    name: 'Customers', displayName: 'Müşteriler & Adaylar (Customers)', category: 'crm', description: 'Müşteri kayıtları',
    fields: [
      { name: 'FullName', label: 'Müşteri Ünvanı / Adı', type: 'text', required: true },
      { name: 'Email', label: 'E-Posta', type: 'text', required: true },
      { name: 'Phone', label: 'Telefon', type: 'text', required: true },
      { name: 'TaxNumber', label: 'Vergi No / TCKN', type: 'text', required: true },
      { name: 'Type', label: 'Müşteri Tipi', type: 'enum', options: [{ value: 1, label: 'Active' }, { value: 0, label: 'Lead' }] },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => {
      const cos = ['Delta Endüstriyel', 'Marmara Çelik']
      const name = `${cos[rand(0, 1)]} A.Ş.`
      return { FullName: name, Email: `info@${name.substring(0, 5).toLowerCase()}.com`, Phone: `+90532${rand(1000000, 9999999)}`, TaxNumber: `${rand(1000000000, 9999999999)}`, Type: 1, IsActive: true }
    },
  },
  {
    name: 'CustomerSites', displayName: 'Müşteri Tesisleri (CustomerSites)', category: 'crm', description: 'Müşteri lokasyonları',
    fields: [
      { name: 'CustomerId', label: 'Müşteri Seçimi', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Name', label: 'Tesis / Şantiye Adı', type: 'text', required: true },
      { name: 'Address', label: 'Açık Adres', type: 'text', required: true },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: (l) => ({ CustomerId: l?.customerId || randId(), Name: `Tesis ${rand(1, 10)}`, Address: `Sanayi Cad. No:${rand(1, 50)}`, IsActive: true }),
  },
  {
    name: 'CustomerAssets', displayName: 'Müşteri Cihazları (CustomerAssets)', category: 'crm', description: 'Tesisteki cihazlar',
    fields: [
      { name: 'SiteId', label: 'Tesis ID', type: 'text', required: true },
      { name: 'Name', label: 'Cihaz Adı', type: 'text', required: true },
      { name: 'SerialNumber', label: 'Seri Numarası', type: 'text', required: true },
      { name: 'InstallationDate', label: 'Kurulum Tarihi', type: 'date' },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => ({ SiteId: randId(), Name: 'Kuru Tip Trafo Panosu', SerialNumber: `SN-${rand(1000, 9999)}`, InstallationDate: new Date().toISOString().split('T')[0], IsActive: true }),
  },

  // 3. SERVICES (Unified Domain)
  {
    name: 'Services', displayName: 'Hizmet Kayıtları (Services)', category: 'services', description: 'Saha servisleri, projeler ve onarım hizmetleri',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Number', label: 'Hizmet Numarası', type: 'text', required: true },
      { name: 'Title', label: 'Hizmet Başlığı', type: 'text', required: true },
      { name: 'Type', label: 'Hizmet Türü', type: 'enum', options: [
        { value: 0, label: 'WorkOrder (İş Emri)' },
        { value: 1, label: 'Project (Proje/Taahhüt)' },
        { value: 2, label: 'Repair (Onarım)' },
      ]},
      { name: 'Status', label: 'Durum', type: 'enum', options: [
        { value: 0, label: 'Draft' }, { value: 1, label: 'Quoted' }, { value: 2, label: 'Accepted' }, 
        { value: 3, label: 'InProgress' }, { value: 4, label: 'Completed' }, { value: 5, label: 'Invoiced' },
        { value: 6, label: 'Cancelled' }, { value: 7, label: 'Rejected' }, { value: 8, label: 'OnHold' }
      ]},
      { name: 'SubStatus', label: 'Alt Durum', type: 'enum', options: [
        { value: 0, label: 'None' }, { value: 1, label: 'WaitingForParts' }, { value: 2, label: 'WaitingForApproval' }
      ]},
      { name: 'Priority', label: 'Öncelik', type: 'enum', options: [
        { value: 0, label: 'Low' }, { value: 1, label: 'Normal' }, { value: 2, label: 'High' }, { value: 3, label: 'Critical' }
      ]},
      { name: 'AssignedUserId', label: 'Atanan Personel', type: 'fk', fkEntity: 'User' },
      { name: 'CurrentTotal', label: 'Toplam Tutar (₺)', type: 'number', required: true },
      { name: 'RequiredDepositPercentage', label: 'Peşinat (%)', type: 'number' },
      { name: 'DepositPaidAmount', label: 'Ödenen Peşinat (₺)', type: 'number' }
    ],
    generateMock: (l) => ({
      CustomerId: l?.customerId || randId(),
      Number: `SRV-26-${rand(1000, 9999)}`,
      Title: 'Trafo Bakım ve Testi',
      Type: 0,
      Status: 2,
      SubStatus: 0,
      Priority: 1,
      AssignedUserId: l?.userId || null,
      CurrentTotal: 15000,
      RequiredDepositPercentage: 0,
      DepositPaidAmount: 0
    }),
  },
  {
    name: 'ServiceItems', displayName: 'Hizmet Kalemleri (ServiceItems)', category: 'services', description: 'Hizmet içindeki malzemeler ve işçilikler',
    fields: [
      { name: 'ServiceId', label: 'Hizmet', type: 'fk', fkEntity: 'Service', required: true },
      { name: 'Kind', label: 'Kalem Türü', type: 'enum', options: [
        { value: 0, label: 'Material (Malzeme)' }, { value: 1, label: 'Labor (İşçilik)' }, { value: 2, label: 'Expense (Masraf)' }
      ]},
      { name: 'Description', label: 'Açıklama', type: 'text', required: true },
      { name: 'Quantity', label: 'Miktar', type: 'number', required: true },
      { name: 'UnitPrice', label: 'Birim Fiyat', type: 'number', required: true },
      { name: 'Unit', label: 'Birim', type: 'text', required: true },
      { name: 'VatRate', label: 'KDV Oranı (%)', type: 'number', required: true },
      { name: 'LineNumber', label: 'Sıra No', type: 'number', required: true }
    ],
    generateMock: (l) => ({
      ServiceId: l?.serviceId || randId(),
      Kind: 0,
      Description: 'Motor Koruma Şalteri',
      Quantity: 1,
      UnitPrice: 450,
      Unit: 'Adet',
      VatRate: 20,
      LineNumber: 1
    }),
  },

  // 4. INVENTORY
  {
    name: 'Warehouses', displayName: 'Depolar & Araçlar (Warehouses)', category: 'inventory', description: 'Depolar',
    fields: [
      { name: 'Name', label: 'Depo Adı', type: 'text', required: true },
      { name: 'Type', label: 'Tür', type: 'enum', options: [{ value: 0, label: 'Main' }, { value: 1, label: 'Van' }, { value: 2, label: 'Virtual' }] },
    ],
    generateMock: () => ({ Name: `Merkez Depo ${rand(1, 9)}`, Type: 0 }),
  },
  {
    name: 'Products', displayName: 'Ürün Kataloğu (Products)', category: 'inventory', description: 'Fiziksel ürünler ve hizmetler',
    fields: [
      { name: 'Code', label: 'Kodu', type: 'text', required: true },
      { name: 'Name', label: 'Adı', type: 'text', required: true },
      { name: 'Unit', label: 'Birim', type: 'text', required: true },
      { name: 'SalePrice', label: 'Satış Fiyatı', type: 'number', required: true },
      { name: 'VatRate', label: 'KDV (%)', type: 'number', required: true },
      { name: 'TracksStock', label: 'Stok Takibi', type: 'boolean' },
      { name: 'IsActive', label: 'Aktif mi?', type: 'boolean' },
    ],
    generateMock: () => ({ Code: `PRD-${rand(100, 999)}`, Name: 'Kontaktör', Unit: 'Adet', SalePrice: 120, VatRate: 20, TracksStock: true, IsActive: true }),
  },
  {
    name: 'MaterialStocks', displayName: 'Depo Stokları (MaterialStocks)', category: 'inventory', description: 'Depodaki ürün miktarları',
    fields: [
      { name: 'WarehouseId', label: 'Depo', type: 'fk', fkEntity: 'Warehouse', required: true },
      { name: 'MaterialCode', label: 'Ürün Kodu', type: 'text', required: true },
      { name: 'Name', label: 'Ürün Adı', type: 'text', required: true },
      { name: 'QuantityOnHand', label: 'Mevcut Miktar', type: 'number', required: true },
      { name: 'ReservedQuantity', label: 'Rezerve Miktar', type: 'number', required: true },
    ],
    generateMock: (l) => ({ WarehouseId: l?.warehouseId || randId(), MaterialCode: `PRD-123`, Name: 'Kontaktör', QuantityOnHand: 50, ReservedQuantity: 0 }),
  },
  {
    name: 'StockMovements', displayName: 'Stok Hareketleri (StockMovements)', category: 'inventory', description: 'Giriş/çıkış hareketleri',
    fields: [
      { name: 'MaterialCode', label: 'Ürün Kodu', type: 'text', required: true },
      { name: 'QuantityDelta', label: 'Değişim Miktarı', type: 'number', required: true },
      { name: 'Direction', label: 'Yön', type: 'enum', options: [{ value: 'IN', label: 'Giriş' }, { value: 'OUT', label: 'Çıkış' }] },
      { name: 'NewQuantity', label: 'Yeni Miktar', type: 'number', required: true },
      { name: 'PreviousQuantity', label: 'Eski Miktar', type: 'number', required: true },
      { name: 'Reason', label: 'Neden', type: 'text', required: true },
      { name: 'SourceWarehouseId', label: 'Depo', type: 'fk', fkEntity: 'Warehouse' },
    ],
    generateMock: (l) => ({ MaterialCode: 'PRD-123', QuantityDelta: 10, Direction: 'IN', PreviousQuantity: 40, NewQuantity: 50, Reason: 'Mal Alımı', SourceWarehouseId: l?.warehouseId || randId() }),
  },

  // 5. FINANCE
  {
    name: 'SalesInvoices', displayName: 'Faturalar (SalesInvoices)', category: 'finance', description: 'Resmi satış faturaları',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'InvoiceNumber', label: 'Fatura No', type: 'text', required: true },
      { name: 'GrandTotal', label: 'Toplam', type: 'number', required: true },
      { name: 'PaidAmount', label: 'Ödenen', type: 'number', required: true },
      { name: 'InvoiceDate', label: 'Tarih', type: 'date', required: true },
      { name: 'Type', label: 'Tür', type: 'enum', options: [{ value: 'Standard', label: 'Standard' }, { value: 'CreditNote', label: 'CreditNote' }] },
    ],
    generateMock: (l) => ({ CustomerId: l?.customerId || randId(), InvoiceNumber: `INV-26-${rand(1000, 9999)}`, GrandTotal: 5000, PaidAmount: 0, InvoiceDate: new Date().toISOString().split('T')[0], Type: 'Standard' }),
  },
  {
    name: 'CustomerPayments', displayName: 'Tahsilatlar (CustomerPayments)', category: 'finance', description: 'Müşterilerden alınan ödemeler',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Amount', label: 'Tutar', type: 'number', required: true },
      { name: 'PaymentMethod', label: 'Yöntem', type: 'enum', options: [{ value: 'BANK_TRANSFER', label: 'Banka' }, { value: 'CARD', label: 'Kart' }, { value: 'CASH', label: 'Nakit' }] },
      { name: 'PaymentDate', label: 'Tarih', type: 'date', required: true },
      { name: 'AllocatedAmount', label: 'Mahsup Edilen', type: 'number', required: true },
    ],
    generateMock: (l) => ({ CustomerId: l?.customerId || randId(), Amount: 5000, PaymentMethod: 'BANK_TRANSFER', PaymentDate: new Date().toISOString().split('T')[0], AllocatedAmount: 0 }),
  },
  {
    name: 'PaymentInvoiceAllocations', displayName: 'Tahsilat Fatura Eşleşmesi (PaymentInvoiceAllocations)', category: 'finance', description: 'Tahsilatın hangi faturayı ödediği',
    fields: [
      { name: 'PaymentId', label: 'Ödeme ID', type: 'text', required: true },
      { name: 'InvoiceId', label: 'Fatura ID', type: 'text', required: true },
      { name: 'Amount', label: 'Tutar', type: 'number', required: true },
    ],
    generateMock: () => ({ PaymentId: randId(), InvoiceId: randId(), Amount: 5000 }),
  },
  {
    name: 'CustomerLedgerEntries', displayName: 'Cari Hareketler (CustomerLedgerEntries)', category: 'finance', description: 'Cari borç/alacak',
    fields: [
      { name: 'CustomerId', label: 'Müşteri', type: 'fk', fkEntity: 'Customer', required: true },
      { name: 'Amount', label: 'Tutar', type: 'number', required: true },
      { name: 'Direction', label: 'Yön', type: 'enum', options: [{ value: 'DEBIT', label: 'Borç (+)' }, { value: 'CREDIT', label: 'Alacak (-)' }] },
      { name: 'BalanceAfter', label: 'Sonraki Bakiye', type: 'number', required: true },
      { name: 'Description', label: 'Açıklama', type: 'text', required: true },
    ],
    generateMock: (l) => ({ CustomerId: l?.customerId || randId(), Amount: 5000, Direction: 'DEBIT', BalanceAfter: 15000, Description: 'Fatura Tahakkuku' }),
  },
  {
    name: 'ProgressBillings', displayName: 'Hakedişler (ProgressBillings)', category: 'finance', description: 'Sözleşmeli hakedişler',
    fields: [
      { name: 'ServiceId', label: 'Hizmet (Proje)', type: 'fk', fkEntity: 'Service', required: true },
      { name: 'BillingNumber', label: 'Hakediş No', type: 'text', required: true },
      { name: 'RequestedAmount', label: 'Talep Edilen', type: 'number', required: true },
      { name: 'ApprovedAmount', label: 'Onaylanan', type: 'number', required: true },
      { name: 'DeductionAmount', label: 'Kesinti', type: 'number', required: true },
      { name: 'IsApproved', label: 'Onaylı mı?', type: 'boolean' },
    ],
    generateMock: (l) => ({ ServiceId: l?.serviceId || randId(), BillingNumber: `PB-01`, RequestedAmount: 100000, ApprovedAmount: 90000, DeductionAmount: 10000, IsApproved: true }),
  },

  // 6. SYSTEM & MAINTENANCE
  {
    name: 'ReminderRecords', displayName: 'Hatırlatıcılar (ReminderRecords)', category: 'system', description: 'Agenda hatırlatıcıları',
    fields: [
      { name: 'EntityId', label: 'Varlık ID', type: 'text', required: true },
      { name: 'EntityName', label: 'Varlık Tipi', type: 'text', required: true },
      { name: 'Type', label: 'Tür', type: 'enum', options: [{ value: 0, label: 'Task' }, { value: 1, label: 'Notification' }] },
      { name: 'Message', label: 'Mesaj', type: 'text', required: true },
      { name: 'DueAt', label: 'Vade', type: 'datetime', required: true },
      { name: 'State', label: 'Durum', type: 'enum', options: [{ value: 0, label: 'Pending' }, { value: 1, label: 'Completed' }, { value: 2, label: 'Dismissed' }] },
      { name: 'CreatedByUserId', label: 'Oluşturan', type: 'text' },
    ],
    generateMock: () => ({ EntityId: randId(), EntityName: 'Service', Type: 0, Message: 'Teknisyen ataması yapılacak', DueAt: new Date().toISOString(), State: 0, CreatedByUserId: randId() }),
  },
  {
    name: 'ReferenceValues', displayName: 'Sistem Değerleri (ReferenceValues)', category: 'system', description: 'Uygulama ayarları',
    fields: [
      { name: 'Domain', label: 'Alan (Domain)', type: 'text', required: true },
      { name: 'Key', label: 'Anahtar', type: 'text', required: true },
      { name: 'Value', label: 'Değer', type: 'text', required: true },
      { name: 'IsActive', label: 'Aktif', type: 'boolean' },
    ],
    generateMock: () => ({ Domain: 'VAT', Key: 'DEFAULT_RATE', Value: '20', IsActive: true }),
  },
  {
    name: 'DocumentCounters', displayName: 'Belge Sayaçları (DocumentCounters)', category: 'system', description: 'Sıradaki numaratör değerleri',
    fields: [
      { name: 'Prefix', label: 'Önek', type: 'text', required: true },
      { name: 'CurrentValue', label: 'Güncel Değer', type: 'number', required: true },
      { name: 'Year', label: 'Yıl', type: 'number', required: true },
    ],
    generateMock: () => ({ Prefix: 'SRV', CurrentValue: 104, Year: 2026 }),
  },
  {
    name: 'CommandRecords', displayName: 'Komut Kayıtları (CommandRecords)', category: 'system', description: 'M9 Audit kayıtları',
    fields: [
      { name: 'Id', label: 'Command ID', type: 'text', required: true },
      { name: 'CommandType', label: 'Tip', type: 'text', required: true },
      { name: 'PayloadJson', label: 'Payload', type: 'text', required: true },
      { name: 'State', label: 'Durum', type: 'text', required: true },
      { name: 'ActorId', label: 'Aktör', type: 'text' },
    ],
    generateMock: () => ({ Id: randId(), CommandType: 'CreateServiceCommand', PayloadJson: '{}', State: 'Completed', ActorId: randId() }),
  },
  {
    name: 'OutboxMessages', displayName: 'Outbox İletileri (OutboxMessages)', category: 'system', description: 'Asenkron olaylar',
    fields: [
      { name: 'EventType', label: 'Olay', type: 'text', required: true },
      { name: 'Payload', label: 'Veri', type: 'text', required: true },
      { name: 'OccurredAt', label: 'Oluşma Zamanı', type: 'datetime', required: true },
      { name: 'ProcessedAt', label: 'İşlenme Zamanı', type: 'datetime' },
    ],
    generateMock: () => ({ EventType: 'ServiceCreatedEvent', Payload: '{}', OccurredAt: new Date().toISOString() }),
  },
  {
    name: 'ExecutionGuards', displayName: 'Idempotency Kilitleri (ExecutionGuards)', category: 'system', description: 'Çoklu istek kalkanları',
    fields: [
      { name: 'Key', label: 'Anahtar', type: 'text', required: true },
      { name: 'ExpiresAt', label: 'Bitiş', type: 'datetime', required: true },
    ],
    generateMock: () => ({ Key: randId(), ExpiresAt: new Date(Date.now() + 3600000).toISOString() }),
  },
  {
    name: 'OperationTraces', displayName: 'İşlem İzleri (OperationTraces)', category: 'system', description: 'HTTP izleme günlükleri',
    fields: [
      { name: 'OperationId', label: 'İşlem ID', type: 'text', required: true },
      { name: 'Action', label: 'Aksiyon', type: 'text', required: true },
      { name: 'StatusCode', label: 'Durum Kodu', type: 'number', required: true },
      { name: 'DurationMilliseconds', label: 'Süre (ms)', type: 'number', required: true },
      { name: 'Outcome', label: 'Sonuç', type: 'text', required: true },
    ],
    generateMock: () => ({ OperationId: randId(), Action: 'GET /api/services', StatusCode: 200, DurationMilliseconds: 42, Outcome: 'Success' }),
  },
  {
    name: 'AuditEvents', displayName: 'Denetim Olayları (AuditEvents)', category: 'system', description: 'Değişiklik geçmişi',
    fields: [
      { name: 'EntityId', label: 'Varlık', type: 'text', required: true },
      { name: 'EntityName', label: 'Tablo', type: 'text', required: true },
      { name: 'Action', label: 'İşlem', type: 'text', required: true },
      { name: 'ChangesJson', label: 'Değişiklikler', type: 'text', required: true },
      { name: 'UserId', label: 'Kullanıcı', type: 'text' },
    ],
    generateMock: () => ({ EntityId: randId(), EntityName: 'Service', Action: 'Modified', ChangesJson: '{}', UserId: randId() }),
  },
  {
    name: 'AuditLogs', displayName: 'Log Kayıtları (AuditLogs)', category: 'system', description: 'Eski sistem logları',
    fields: [
      { name: 'Action', label: 'İşlem', type: 'text', required: true },
      { name: 'EntityId', label: 'Kayıt ID', type: 'text', required: true },
      { name: 'EntityName', label: 'Tablo Adı', type: 'text', required: true },
    ],
    generateMock: () => ({ Action: 'Create', EntityId: randId(), EntityName: 'Customer' }),
  },
]
