import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

export interface PaymentDto { id: string; customerId: string; amount: number; paymentMethod: string; paymentDate: string; }
/** A payment as a row of the list of everyone's payments. */
export interface PaymentRow { id: string; customerId: string; customerName: string; amount: number; paymentMethod: string; paymentDate: string; }
/** An invoice as a row of the list of everyone's invoices. */
export interface SalesInvoiceRow { id: string; customerId: string; customerName: string; invoiceNumber: string; grandTotal: number; paidAmount: number; appliedDepositAmount: number; remainingAmount: number; invoiceDate: string; }
export interface PaymentAllocationDto { paymentId: string; invoiceId: string; amount: number; }


export const paymentsApi = {
  /** Everyone's payments, newest first; `customerId` narrows them to one customer. */
  list: (params: { customerId?: string; limit?: number; offset?: number }): Promise<ApiPage<PaymentRow>> =>
    apiRequestPaged<PaymentRow>(`/api/payments${queryString(params)}`),
  /** Everyone's invoices, newest first; `customerId` narrows them to one customer. */
  listInvoices: (params: { customerId?: string; limit?: number; offset?: number }): Promise<ApiPage<SalesInvoiceRow>> =>
    apiRequestPaged<SalesInvoiceRow>(`/api/payments/invoices${queryString(params)}`),
  create: (payload: { customerId: string; amount: number; paymentMethod: string; paymentDate: string }) =>
    apiRequest<PaymentDto>('/api/payments', { method: 'POST', body: JSON.stringify(payload) }),
  allocate: (payload: { paymentId: string; invoiceId: string; amount: number }) =>
    apiRequest<PaymentAllocationDto>('/api/payments/allocate', { method: 'POST', body: JSON.stringify(payload) }),
}

