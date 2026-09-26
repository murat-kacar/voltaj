export type EntityTableType =
  | 'users'
  | 'customers'
  | 'work-orders'
  | 'quotes'
  | 'products'
  | 'invoices'
  | 'payments'
  | 'projects'
  | 'reminders'

export interface EntityFieldOption {
  value: string | number | boolean
  label: string
  color?: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'
}

export const USER_ROLES: EntityFieldOption[] = [
  { value: 'Admin', label: 'Admin (Yönetici)', color: 'error' },
  { value: 'Manager', label: 'Manager (Müdür)', color: 'primary' },
  { value: 'Technician', label: 'Technician (Teknisyen)', color: 'warning' },
  { value: 'Viewer', label: 'Viewer (İzleyici)', color: 'default' },
]

export const CUSTOMER_TYPES: EntityFieldOption[] = [
  { value: 'Active', label: 'Active (Kazanılmış Müşteri)', color: 'success' },
  { value: 'Lead', label: 'Lead (Aday Müşteri)', color: 'info' },
]

export const QUOTE_STATES: EntityFieldOption[] = [
  { value: 'Draft', label: 'Draft (Taslak)', color: 'default' },
  { value: 'Issued', label: 'Issued (Gönderildi)', color: 'info' },
  { value: 'Accepted', label: 'Accepted (Onaylandı)', color: 'success' },
  { value: 'Rejected', label: 'Rejected (Reddedildi)', color: 'error' },
  { value: 'Expired', label: 'Expired (Süresi Doldu)', color: 'warning' },
]

export const WORK_ORDER_STATUSES: EntityFieldOption[] = [
  { value: 'Open', label: 'Open (Açık)', color: 'default' },
  { value: 'Assigned', label: 'Assigned (Atandı)', color: 'info' },
  { value: 'EnRoute', label: 'EnRoute (Yolda)', color: 'secondary' },
  { value: 'InProgress', label: 'InProgress (Devam Ediyor)', color: 'warning' },
  { value: 'OnHold', label: 'OnHold (Beklemede)', color: 'error' },
  { value: 'Completed', label: 'Completed (Tamamlandı)', color: 'success' },
  { value: 'ReadyForBilling', label: 'ReadyForBilling (Faturaya Hazır)', color: 'primary' },
  { value: 'Invoiced', label: 'Invoiced (Faturalandı)', color: 'success' },
  { value: 'Cancelled', label: 'Cancelled (İptal)', color: 'error' },
  { value: 'NoShow', label: 'NoShow (Adreste Yok)', color: 'error' },
]

export const PAYMENT_METHODS: EntityFieldOption[] = [
  { value: 'BankTransfer', label: 'Banka Havalesi / EFT', color: 'primary' },
  { value: 'Card', label: 'Kredi / Banka Kartı', color: 'success' },
  { value: 'Cash', label: 'Nakit Kasa', color: 'warning' },
]

export const INVOICE_TYPES: EntityFieldOption[] = [
  { value: 'Standard', label: 'Standard Satış Faturası', color: 'primary' },
  { value: 'CreditNote', label: 'CreditNote (İade/Alacak Dekontu)', color: 'warning' },
]

export const PRODUCT_UNITS: EntityFieldOption[] = [
  { value: 'Adet', label: 'Adet' },
  { value: 'Saat', label: 'Saat (İşçilik)' },
  { value: 'Metre', label: 'Metre' },
  { value: 'Rulo', label: 'Rulo' },
  { value: 'Kg', label: 'Kg' },
  { value: 'Paket', label: 'Paket' },
]

const FIRST_NAMES = ['Ahmet', 'Mehmet', 'Ayşe', 'Fatma', 'Ali', 'Burak', 'Cem', 'Deniz', 'Elif', 'Gökhan', 'Hakan', 'İrem', 'Kerem', 'Murat', 'Onur', 'Pelin', 'Serkan', 'Zeynep']
const LAST_NAMES = ['Yılmaz', 'Kaya', 'Demir', 'Çelik', 'Şahin', 'Yıldız', 'Yıldırım', 'Öztürk', 'Aydın', 'Özdemir', 'Arslan', 'Doğan', 'Kılıç', 'Aslan', 'Çetin', 'Kara', 'Koç', 'Kurt']
const COMPANIES = ['Mühendislik', 'Elektrik A.Ş.', 'Sanayi ve Ticaret Ltd.', 'İnşaat Taahhüt', 'Otomasyon Sistemleri', 'Enerji ve Tesisat', 'Lojistik Depoculuk', 'Teknoloji Grubu']
const JOB_TITLES = ['Pano Kurulumu ve Şalt Revizyonu', 'Acil Kaçak Akım Arızası Müdahalesi', 'Kompanzasyon Kondansatör Değişimi', 'Trafo Merkezi Yıllık Temizlik ve Bakım', 'Kablo Tavası ve Hat Çekimi', 'Topraklama Ölçümü ve Raporlama', 'Jeneratör Senkronizasyon Panosu Montajı', 'UPS ve Kesintisiz Güç Kaynağı Devreye Alma']
const PRODUCT_NAMES = ['Schneider 16A 1P B Tipi Sigorta', '3x2.5 NYM Antigron Kablo (100m)', 'Siemens 40A 30mA Kaçak Akım Rölesi', '50 kVAr Güç Kondansatörü', 'Legrand 25A 3P Kontaktör', 'Usta Elektrik Teknisyeni Saatlik İşçilik', 'Termal Kamera ile Pano Ölçüm Raporu', 'LED Endüstriyel Etanj Projektör 150W']

function pick<T>(list: T[]): T {
  return list[Math.floor(Math.random() * list.length)]
}

function randInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min
}

export function generateMockUser() {
  const first = pick(FIRST_NAMES)
  const last = pick(LAST_NAMES)
  const role = pick(USER_ROLES).value as string
  const num = randInt(10, 99)
  return {
    name: `${first} ${last}`,
    email: `${first.toLowerCase()}.${last.toLowerCase()}${num}@voltflow-test.com`,
    password: 'TestPassword123!',
    role,
    isApproved: Math.random() > 0.2, // %80 onaylı
  }
}

export function generateMockCustomer() {
  const contact = `${pick(FIRST_NAMES)} ${pick(LAST_NAMES)}`
  const company = `${contact} (${pick(LAST_NAMES)} ${pick(COMPANIES)})`
  const num = randInt(100, 999)
  return {
    fullName: company,
    email: `iletisim@${pick(LAST_NAMES).toLowerCase()}enerji${num}.com`,
    phone: `+9053${randInt(10, 99)}${randInt(100, 999)}${randInt(10, 99)}`,
    taxNumber: `${randInt(1000000000, 9999999999)}`,
    type: pick(CUSTOMER_TYPES).value as 'Lead' | 'Active',
    isActive: Math.random() > 0.15,
  }
}

export function generateMockQuote(customerId?: string) {
  const num = randInt(100, 999)
  const state = pick(QUOTE_STATES).value as string
  return {
    customerId: customerId ?? '',
    title: `${pick(JOB_TITLES)} - Tasarım Teklifi`,
    total: randInt(15, 120) * 1000,
    state,
    rejectionReason: state === 'Rejected' ? 'Müşteri bütçe sınırını aştığı için revize istedi.' : undefined,
    itemDescription: pick(PRODUCT_NAMES),
    itemQuantity: randInt(1, 20),
    itemUnitPrice: randInt(5, 80) * 100,
    itemKind: 'Material',
    number: `QT-2026-${num}`,
  }
}

export function generateMockWorkOrder(customerId?: string, assignedUserId?: string) {
  const num = randInt(100, 999)
  const status = pick(WORK_ORDER_STATUSES).value as string
  return {
    customerId: customerId ?? '',
    assignedUserId: assignedUserId ?? '',
    title: pick(JOB_TITLES),
    status,
    total: randInt(5, 65) * 1000,
    isSafetyChecklistCompleted: status !== 'Open',
    holdReason: status === 'OnHold' ? 'Yedek parça siparişi bekleniyor' : undefined,
    cancellationReason: status === 'Cancelled' ? 'Müşteri talebiyle iptal edildi' : status === 'NoShow' ? 'Müşteri adreste bulunamadı' : undefined,
    number: `WO-2026-${num}`,
  }
}

export function generateMockProduct() {
  const num = randInt(100, 999)
  const isLabor = Math.random() > 0.7
  return {
    code: `PRD-${isLabor ? 'SRV' : 'MTR'}-${num}`,
    name: isLabor ? 'Usta Elektrik Teknisyeni Saatlik İşçilik' : pick(PRODUCT_NAMES),
    barcode: isLabor ? '' : `869${randInt(100000000, 999999999)}`,
    unit: isLabor ? 'Saat' : pick(PRODUCT_UNITS).value as string,
    salePrice: randInt(15, 250) * 50,
    vatRate: 20,
    tracksStock: !isLabor,
    isActive: true,
  }
}

export function generateMockPayment(customerId?: string) {
  const method = pick(PAYMENT_METHODS).value as 'BankTransfer' | 'Card' | 'Cash'
  const amount = randInt(5, 50) * 1000
  return {
    customerId: customerId ?? '',
    amount,
    paymentMethod: method,
    paymentDate: new Date().toISOString().split('T')[0],
    allocatedAmount: Math.random() > 0.3 ? amount : 0,
  }
}

export function generateMockInvoice(customerId?: string) {
  const num = randInt(100, 999)
  const total = randInt(10, 80) * 1000
  const isPaid = Math.random() > 0.5
  return {
    customerId: customerId ?? '',
    invoiceNumber: `INV-2026-${num}`,
    grandTotal: total,
    paidAmount: isPaid ? total : 0,
    type: pick(INVOICE_TYPES).value as 'Standard' | 'CreditNote',
    invoiceDate: new Date().toISOString().split('T')[0],
  }
}

export function generateMockProject(customerId?: string) {
  const num = randInt(10, 99)
  return {
    customerId: customerId ?? '',
    name: `${pick(JOB_TITLES)} Projesi - Faz ${num}`,
    budget: randInt(250, 2000) * 1000,
  }
}

export function generateMockReminder(customerId?: string) {
  const future = new Date()
  future.setDate(future.getDate() + randInt(1, 14))
  return {
    type: 'CUSTOMER_FOLLOWUP',
    entityName: 'Customer',
    entityId: customerId ?? '',
    dueAt: future.toISOString(),
    message: `${pick(JOB_TITLES)} için müşteri ile periyodik kontrol ve teklif değerlendirme görüşmesi`,
  }
}
