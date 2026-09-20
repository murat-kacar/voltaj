export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  operationId?: string
  errorCode?: string
}

export type AuthResult = {
  userId: string
  name: string
  email: string
  token: string
  isApproved: boolean
}

export type WorkOrderTimeEntry = {
  checkInTime: string
  checkOutTime?: string | null
  notes?: string | null
}

export type WorkOrderItem = {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type WorkOrder = {
  id: string
  customerId: string
  assignedUserId?: string | null
  number: string
  title: string
  total: number
  status: string
  isSafetyChecklistCompleted: boolean
  holdReason?: string | null
  cancellationReason?: string | null
  targetCompletionDate?: string | null
  sourceQuoteId?: string | null
  siteId?: string | null
  assetId?: string | null
  signatureData?: string | null
  proofOfWorkPhotoUrl?: string | null
  timeEntries: WorkOrderTimeEntry[]
  items: WorkOrderItem[]
}

export type Customer = {
  id: string
  fullName: string
  email: string
  phone: string
  taxNumber: string
  /** Lead: someone who has not bought yet. Active: a customer. */
  type: 'Lead' | 'Active'
  isActive: boolean
  createdAt: string
}

export type CustomerInput = { fullName: string; email?: string; phone: string; taxNumber?: string }

export type CustomerAsset = { id: string; siteId: string; name: string; serialNumber: string; installationDate?: string | null; isActive: boolean }

export type CustomerSite = { id: string; customerId: string; name: string; address: string; isActive: boolean; assets: CustomerAsset[] }

export type SiteInput = { name: string; address: string; isActive: boolean }

export type AssetInput = { name: string; serialNumber?: string; installationDate?: string | null; isActive: boolean }
export type QuoteState = 'Draft' | 'Issued' | 'Accepted' | 'Rejected' | 'Expired'

/** What a line of a quote is for; it groups the lines of the document. */
export type QuoteLineKind = 'Material' | 'Labor' | 'Service'

export type QuoteLine = {
  id: string
  lineNumber: number
  kind: QuoteLineKind
  description: string
  unit: string
  quantity: number
  /** What the customer pays per unit, VAT included. */
  unitPrice: number
  vatRate: number
  lineTotal: number
  /** The VAT that is already inside the line total. */
  vatAmount: number
}

/** A row of the quote list. */
export type QuoteSummary = {
  id: string
  customerId: string
  customerName: string
  number: string
  title: string
  state: QuoteState
  total: number
  /** The last day the customer can accept, as a calendar day ("2026-09-30"). */
  validUntil: string | null
  createdAt: string
  requiredDepositAmount: number
  depositPaidAmount: number
}

/** A whole quote, with what the screen and the printed document need. Prices are what the customer pays, VAT included. */
export type Quote = {
  id: string
  customerId: string
  customerName: string
  customerPhone: string
  customerEmail: string
  customerTaxNumber: string
  number: string
  title: string
  notes: string | null
  state: QuoteState
  total: number
  vatTotal: number
  createdAt: string
  issuedAt: string | null
  validUntil: string | null
  decidedAt: string | null
  rejectionReason: string | null
  requiredDepositPercentage: number
  requiredDepositAmount: number
  depositPaidAmount: number
  siteId: string | null
  siteName: string | null
  siteAddress: string | null
  assetId: string | null
  assetName: string | null
  workOrderId: string | null
  workOrderNumber: string | null
  items: QuoteLine[]
}

export type QuoteLineInput = { kind: QuoteLineKind; description: string; quantity: number; unitPrice: number; unit: string; vatRate: number }

/** The draft of a quote: its details and the whole list of its lines. */
export type QuoteInput = {
  title: string
  notes?: string | null
  validUntil?: string | null
  siteId?: string | null
  assetId?: string | null
  items: QuoteLineInput[]
}

export class ApiError extends Error {
  problemDetails?: ProblemDetails
  status: number

  constructor(message: string, status: number, problemDetails?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problemDetails = problemDetails
  }
}

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const session = localStorage.getItem('voltflow.session')
  const token = session ? (JSON.parse(session) as AuthResult).token : undefined
  const method = init.method ?? 'GET'
  const response = await fetch(`${apiBase}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      'X-Client-Screen': window.location.pathname,
      'X-Client-Action': `${method} ${path}`,
      ...(method !== 'GET' ? { 'Idempotency-Key': crypto.randomUUID() } : {}),
      ...(init.headers ?? {}),
    },
  })

  if (!response.ok) {
    if (response.status === 401) {
      localStorage.removeItem('voltflow.session')
      window.location.reload()
    }
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    const message = problem.detail || problem.title || `Request failed with status ${response.status}`
    throw new ApiError(message, response.status, problem)
  }

  return response
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await apiFetch(path, init)
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export type ApiPage<T> = { items: T[]; total: number }

/** A list endpoint that pages: the items come in the body, the size of the whole result in the X-Total-Count header. */
export async function apiRequestPaged<T>(path: string): Promise<ApiPage<T>> {
  const response = await apiFetch(path)
  const items = (await response.json()) as T[]
  const total = Number(response.headers.get('X-Total-Count'))
  return { items, total: Number.isFinite(total) && total >= items.length ? total : items.length }
}

/** Builds a query string from the parameters that have a value. */
export function queryString(params: Record<string, string | number | boolean | undefined | null>): string {
  const parts = Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== '')
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
  return parts.length > 0 ? `?${parts.join('&')}` : ''
}

/** The roles inside the signed-in user's token. The API decides what is allowed; this only lets the screen hide what would be refused. */
export function sessionRoles(): string[] {
  try {
    const session = localStorage.getItem('voltflow.session')
    const token = session ? (JSON.parse(session) as AuthResult).token : undefined
    const payload = token?.split('.')[1]
    if (!payload) return []
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const claims = JSON.parse(json) as Record<string, unknown>
    const raw = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? claims.role ?? []
    return Array.isArray(raw) ? raw.map(String) : [String(raw)]
  } catch {
    return []
  }
}

export const authApi = {
  login: (email: string, password: string) =>
    apiRequest<AuthResult>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  register: (payload: { name: string; email: string; password: string; otp?: string }) =>
    apiRequest<AuthResult>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  requestPasswordReset: (email: string) =>
    apiRequest<void>('/api/auth/password-reset/request', {
      method: 'POST',
      body: JSON.stringify({ email }),
    }),
  completePasswordReset: (payload: { token: string; newPassword: string; email?: string }) =>
    apiRequest<void>('/api/auth/password-reset/complete', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  revokeSession: (token: string) =>
    apiRequest<void>('/api/auth/session/revoke', {
      method: 'POST',
      body: JSON.stringify({ token }),
    }),
}


export const customersApi = {
  /** The first page, for the places that only need something to pick from. */
  list: () => apiRequest<Customer[]>('/api/customers'),
  page: (params: { search?: string; type?: string; active?: boolean; limit?: number; offset?: number }) =>
    apiRequestPaged<Customer>(`/api/customers${queryString(params)}`),
  get: (id: string) => apiRequest<Customer>(`/api/customers/${id}`),
  create: (payload: CustomerInput) => apiRequest<Customer>('/api/customers', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: CustomerInput) => apiRequest<Customer>(`/api/customers/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  activate: (id: string) => apiRequest<Customer>(`/api/customers/${id}/activate`, { method: 'POST' }),
  deactivate: (id: string) => apiRequest<Customer>(`/api/customers/${id}/deactivate`, { method: 'POST' }),
  convertToActive: (id: string) => apiRequest<Customer>(`/api/customers/${id}/convert-to-active`, { method: 'POST' }),
  sites: (id: string) => apiRequest<CustomerSite[]>(`/api/customers/${id}/sites`),
  createSite: (id: string, payload: SiteInput) =>
    apiRequest<CustomerSite>(`/api/customers/${id}/sites`, { method: 'POST', body: JSON.stringify(payload) }),
  updateSite: (id: string, siteId: string, payload: SiteInput) =>
    apiRequest<CustomerSite>(`/api/customers/${id}/sites/${siteId}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createAsset: (id: string, siteId: string, payload: AssetInput) =>
    apiRequest<CustomerAsset>(`/api/customers/${id}/sites/${siteId}/assets`, { method: 'POST', body: JSON.stringify(payload) }),
  updateAsset: (id: string, siteId: string, assetId: string, payload: AssetInput) =>
    apiRequest<CustomerAsset>(`/api/customers/${id}/sites/${siteId}/assets/${assetId}`, { method: 'PUT', body: JSON.stringify(payload) }),
}

export const quotesApi = {
  page: (params: { search?: string; state?: string; customerId?: string; limit?: number; offset?: number }) =>
    apiRequestPaged<QuoteSummary>(`/api/quotes${queryString(params)}`),
  get: (id: string) => apiRequest<Quote>(`/api/quotes/${id}`),
  create: (payload: QuoteInput & { customerId: string }) => apiRequest<Quote>('/api/quotes', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: QuoteInput) => apiRequest<Quote>(`/api/quotes/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id: string) => apiRequest<void>(`/api/quotes/${id}`, { method: 'DELETE' }),
  /** A new draft with the same customer, address and lines: how a quote is revised. */
  copy: (id: string) => apiRequest<Quote>(`/api/quotes/${id}/copy`, { method: 'POST' }),
  issue: (id: string) => apiRequest<Quote>(`/api/quotes/${id}/issue`, { method: 'POST' }),
  accept: (id: string, requiredDepositPercentage: number) =>
    apiRequest<Quote>(`/api/quotes/${id}/accept`, { method: 'POST', body: JSON.stringify({ requiredDepositPercentage }) }),
  payDeposit: (id: string, amount: number) =>
    apiRequest<Quote>(`/api/quotes/${id}/pay-deposit`, { method: 'POST', body: JSON.stringify({ amount }) }),
  reject: (id: string, reason: string) =>
    apiRequest<Quote>(`/api/quotes/${id}/reject`, { method: 'POST', body: JSON.stringify({ reason }) }),
  /** Withdraws a quote that nobody has decided on: it can no longer be accepted. */
  expire: (id: string) => apiRequest<Quote>(`/api/quotes/${id}/expire`, { method: 'POST' }),
  convertToWorkOrder: (id: string) => apiRequest<WorkOrder>(`/api/quotes/${id}/work-order`, { method: 'POST' }),
}

export const workOrdersApi = {
  list: (params?: { limit?: number; offset?: number }) =>
    apiRequestPaged<WorkOrder>(`/api/workorders${queryString(params ?? {})}`),
  getById: (id: string) => apiRequest<WorkOrder>(`/api/workorders/${id}`),
  create: (payload: { customerId: string; title: string }) =>
    apiRequest<WorkOrder>('/api/workorders', { method: 'POST', body: JSON.stringify(payload) }),
  assign: (id: string, employeeUserId: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/assign`, { method: 'POST', body: JSON.stringify({ employeeUserId }) }),
  enRoute: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/en-route`, { method: 'POST' }),
  noShow: (id: string, reason: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/no-show`, { method: 'POST', body: JSON.stringify({ reason }) }),
  safetyChecklist: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/safety-checklist`, { method: 'POST' }),
  start: (id: string, targetCompletionDate?: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/start`, { method: 'POST', body: JSON.stringify({ targetCompletionDate: targetCompletionDate ?? null }) }),
  checkIn: (id: string, notes?: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/check-in`, { method: 'POST', body: JSON.stringify({ notes: notes ?? null }) }),
  checkOut: (id: string, notes?: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/check-out`, { method: 'POST', body: JSON.stringify({ notes: notes ?? null }) }),
  hold: (id: string, reason: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/hold`, { method: 'POST', body: JSON.stringify({ reason }) }),
  resume: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/resume`, { method: 'POST' }),
  cancel: (id: string, reason: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/cancel`, { method: 'POST', body: JSON.stringify({ reason }) }),
  complete: (id: string, signatureData?: string, proofOfWorkPhotoUrl?: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/complete`, { method: 'POST', body: JSON.stringify({ signatureData: signatureData ?? null, proofOfWorkPhotoUrl: proofOfWorkPhotoUrl ?? null }) }),
  approveBilling: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/approve-billing`, { method: 'POST' }),
  invoice: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/invoice`, { method: 'POST' }),
  addItem: (id: string, description: string, quantity: number, unitPrice: number) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/items`, { method: 'POST', body: JSON.stringify({ description, quantity, unitPrice }) }),
}
export interface StockDto { materialCode: string; name: string; quantityOnHand: number; reservedQuantity: number; availableQuantity: number; }
export interface PaymentDto { id: string; customerId: string; amount: number; paymentMethod: string; paymentDate: string; }
export interface SalesInvoiceDto { id: string; customerId: string; invoiceNumber: string; grandTotal: number; paidAmount: number; appliedDepositAmount: number; remainingAmount: number; invoiceDate: string; }
export interface PaymentAllocationDto { paymentId: string; invoiceId: string; amount: number; }
export interface ProjectPhaseDto { id: string; title: string; plannedAmount: number; }
export interface ProjectDto { id: string; customerId: string; number: string; name: string; budget: number; phases: ProjectPhaseDto[]; }
export interface BillingEntryDto { id: string; projectId: string; customerId: string; amount: number; }
export interface AuditLogDto { id: string; userId: string; action: string; entityName: string; entityId: string; details: string; timestamp: string; }
export const inventoryApi = { getByMaterialCode: (code: string) => apiRequest<StockDto>(`/api/inventory/${code}`), adjust: (payload: { materialCode: string; delta: number }) => apiRequest<StockDto>('/api/inventory/adjust', { method: 'POST', body: JSON.stringify(payload) }) };
export const paymentsApi = {
  listByCustomer: (customerId: string, params?: { limit?: number; offset?: number }) =>
    apiRequestPaged<PaymentDto>(`/api/payments/${customerId}${queryString(params ?? {})}`),
  listInvoicesByCustomer: (customerId: string, params?: { limit?: number; offset?: number }) =>
    apiRequestPaged<SalesInvoiceDto>(`/api/payments/invoices/${customerId}${queryString(params ?? {})}`),
  create: (payload: { customerId: string; amount: number; paymentMethod: string; paymentDate: string }) =>
    apiRequest<PaymentDto>('/api/payments', { method: 'POST', body: JSON.stringify(payload) }),
  allocate: (payload: { paymentId: string; invoiceId: string; amount: number }) =>
    apiRequest<PaymentAllocationDto>('/api/payments/allocate', { method: 'POST', body: JSON.stringify(payload) }),
};
export const projectsApi = {
  list: (params?: { limit?: number; offset?: number }) =>
    apiRequestPaged<ProjectDto>(`/api/projects${queryString(params ?? {})}`),
  getById: (id: string) => apiRequest<ProjectDto>(`/api/projects/${id}`),
  create: (payload: { customerId: string; name: string; budget: number }) =>
    apiRequest<ProjectDto>('/api/projects', { method: 'POST', body: JSON.stringify(payload) }),
  addPhase: (id: string, payload: { title: string; plannedAmount: number }) =>
    apiRequest<ProjectDto>(`/api/projects/${id}/phases`, { method: 'POST', body: JSON.stringify(payload) }),
};
export const billingApi = {
  list: (projectId: string, params?: { limit?: number; offset?: number }) =>
    apiRequestPaged<BillingEntryDto>(`/api/billing/${projectId}${queryString(params ?? {})}`),
  create: (projectId: string, payload: { customerId: string; amount: number }) =>
    apiRequest<BillingEntryDto>(`/api/billing/${projectId}`, { method: 'POST', body: JSON.stringify(payload) }),
};
export const auditLogsApi = { listRecent: () => apiRequest<AuditLogDto[]>('/api/audit-logs/recent') };

// ---- Quick sale: price list, sales at the counter, shifts ---------------------------------------------------

export type PaymentMethod = 'Cash' | 'Card' | 'BankTransfer'

export type Product = {
  id: string
  code: string
  name: string
  barcode?: string | null
  unit: string
  salePrice: number
  vatRate: number
  tracksStock: boolean
  isActive: boolean
  stockAvailable?: number | null
}

export type ProductInput = {
  code: string
  name: string
  barcode?: string
  unit?: string
  salePrice: number
  vatRate: number
  tracksStock: boolean
}

export type ProductUpdate = Omit<ProductInput, 'code'> & { isActive: boolean }

export type SaleLineRequest = {
  productId?: string | null
  description?: string
  quantity: number
  unitPrice?: number
  vatRate?: number
  discountAmount: number
}

export type SalePaymentRequest = { method: PaymentMethod; amount: number; reference?: string }

export type CreateSaleRequest = {
  customerId?: string | null
  lines: SaleLineRequest[]
  receiptDiscount: number
  payments: SalePaymentRequest[]
  note?: string
}

export type SaleLine = {
  id: string
  productId?: string | null
  productCode?: string | null
  barcode?: string | null
  description: string
  unit: string
  quantity: number
  unitPrice: number
  vatRate: number
  lineDiscount: number
  receiptDiscountShare: number
  lineTotal: number
  vatAmount: number
  returnedQuantity: number
}

export type SalePayment = { method: PaymentMethod; amount: number; tendered: number; reference?: string | null }

export type SaleReturnLine = { quickSaleLineId: string; productCode?: string | null; description: string; quantity: number; refundAmount: number }

export type SaleReturn = {
  id: string
  returnNumber: string
  returnedAt: string
  cashierName: string
  reason: string
  refundMethod: PaymentMethod
  refundTotal: number
  lines: SaleReturnLine[]
}

export type QuickSale = {
  id: string
  saleNumber: string
  soldAt: string
  cashierUserId: string
  cashierName: string
  shiftId: string
  customerId?: string | null
  customerName?: string | null
  status: 'Completed' | 'Voided'
  subtotal: number
  lineDiscountTotal: number
  receiptDiscount: number
  grandTotal: number
  vatTotal: number
  cashTendered: number
  changeGiven: number
  note?: string | null
  voidedAt?: string | null
  voidReason?: string | null
  lines: SaleLine[]
  payments: SalePayment[]
  returns: SaleReturn[]
}

export type QuickSaleSummary = {
  id: string
  saleNumber: string
  soldAt: string
  cashierName: string
  customerId?: string | null
  status: 'Completed' | 'Voided'
  grandTotal: number
  vatTotal: number
  paymentMethods: PaymentMethod[]
  hasReturns: boolean
}

export type CashShift = {
  id: string
  cashierUserId: string
  cashierName: string
  openedAt: string
  openingCash: number
  status: 'Open' | 'Closed'
  closedAt?: string | null
  countedCash?: number | null
  expectedCash?: number | null
  cashDifference?: number | null
  note?: string | null
}

export type VatBucket = { rate: number; gross: number; vat: number }

export type ShiftReport = {
  shift: CashShift
  saleCount: number
  voidedCount: number
  salesTotal: number
  discountTotal: number
  vatTotal: number
  cashSales: number
  cardSales: number
  transferSales: number
  returnCount: number
  returnTotal: number
  cashRefunds: number
  netSales: number
  expectedCash: number
  vatBreakdown: VatBucket[]
}

export const productsApi = {
  list: (params: { search?: string; activeOnly?: boolean; limit?: number; offset?: number }) =>
    apiRequestPaged<Product>(`/api/products${queryString(params)}`),
  lookup: (term: string) => apiRequest<Product>(`/api/products/lookup${queryString({ term })}`),
  create: (payload: ProductInput) => apiRequest<Product>('/api/products', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: ProductUpdate) => apiRequest<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
}

export const quickSalesApi = {
  list: (params: { search?: string; status?: string; from?: string; to?: string; customerId?: string; limit?: number; offset?: number }) =>
    apiRequestPaged<QuickSaleSummary>(`/api/quick-sales${queryString(params)}`),
  get: (id: string) => apiRequest<QuickSale>(`/api/quick-sales/${id}`),
  create: (payload: CreateSaleRequest) => apiRequest<QuickSale>('/api/quick-sales', { method: 'POST', body: JSON.stringify(payload) }),
  void: (id: string, reason: string) =>
    apiRequest<QuickSale>(`/api/quick-sales/${id}/void`, { method: 'POST', body: JSON.stringify({ reason }) }),
  return: (id: string, payload: { reason: string; refundMethod: PaymentMethod; items: { lineId: string; quantity: number }[] }) =>
    apiRequest<QuickSale>(`/api/quick-sales/${id}/returns`, { method: 'POST', body: JSON.stringify(payload) }),
}

export const cashShiftsApi = {
  /** Resolves to null when the signed-in user has no open shift. */
  current: async () => (await apiRequest<ShiftReport | undefined>('/api/cash-shifts/current')) ?? null,
  open: (openingCash: number) =>
    apiRequest<ShiftReport>('/api/cash-shifts/open', { method: 'POST', body: JSON.stringify({ openingCash }) }),
  close: (id: string, countedCash: number, note?: string) =>
    apiRequest<ShiftReport>(`/api/cash-shifts/${id}/close`, { method: 'POST', body: JSON.stringify({ countedCash, note }) }),
  report: (id: string) => apiRequest<ShiftReport>(`/api/cash-shifts/${id}/report`),
  list: (params: { limit?: number; offset?: number }) => apiRequestPaged<CashShift>(`/api/cash-shifts${queryString(params)}`),
}

export interface ReminderDto {
  id: string
  type: string
  entityName: string
  entityId: string
  dueAt: string
  message: string
  state: 'Pending' | 'Completed' | 'Dismissed'
  attempts: number
  nextAttemptAt: string
  completionNote?: string | null
}

export const remindersApi = {
  list: (params?: { state?: string; limit?: number; offset?: number }) =>
    apiRequestPaged<ReminderDto>(`/api/reminders${queryString(params ?? {})}`),
  create: (payload: { type: string; entityName: string; entityId: string; dueAt: string; message: string }) =>
    apiRequest<ReminderDto>('/api/reminders', { method: 'POST', body: JSON.stringify(payload) }),
  dismiss: (id: string, note?: string) =>
    apiRequest<ReminderDto>(`/api/reminders/${id}/dismiss`, { method: 'POST', body: JSON.stringify({ note }) }),
  complete: (id: string, note?: string) =>
    apiRequest<ReminderDto>(`/api/reminders/${id}/complete`, { method: 'POST', body: JSON.stringify({ note }) }),
}
