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

export type VatBucket = { rate: number; gross: number; vat: number }

export const productsApi = {
  list: (params: { search?: string; activeOnly?: boolean; limit?: number; offset?: number }): Promise<ApiPage<Product>> =>
    apiRequestPaged<Product>(`/api/products${queryString(params)}`),
  lookup: (term: string) => apiRequest<Product>(`/api/products/lookup${queryString({ term })}`),
  create: (payload: ProductInput) => apiRequest<Product>('/api/products', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: ProductUpdate) => apiRequest<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
}

export const salesApi = {
  createQuick: (payload: CreateSaleRequest & { shiftId?: string }) => 
    apiRequest<{ id: string; saleNumber: string }>('/api/sales/quick', { method: 'POST', body: JSON.stringify(payload) }),
}

