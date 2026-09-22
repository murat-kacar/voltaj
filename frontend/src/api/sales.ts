import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

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
  list: (params: { search?: string; activeOnly?: boolean; limit?: number; offset?: number }): Promise<ApiPage<Product>> =>
    apiRequestPaged<Product>(`/api/products${queryString(params)}`),
  lookup: (term: string) => apiRequest<Product>(`/api/products/lookup${queryString({ term })}`),
  create: (payload: ProductInput) => apiRequest<Product>('/api/products', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: ProductUpdate) => apiRequest<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
}

export const quickSalesApi = {
  list: (params: { search?: string; status?: string; from?: string; to?: string; customerId?: string; limit?: number; offset?: number }): Promise<ApiPage<QuickSaleSummary>> =>
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
  list: (params: { limit?: number; offset?: number }): Promise<ApiPage<CashShift>> =>
    apiRequestPaged<CashShift>(`/api/cash-shifts${queryString(params)}`),
}
