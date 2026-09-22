import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'
import type { WorkOrder } from './workOrders'

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

export const quotesApi = {
  page: (params: { search?: string; state?: string; customerId?: string; limit?: number; offset?: number }): Promise<ApiPage<QuoteSummary>> =>
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
