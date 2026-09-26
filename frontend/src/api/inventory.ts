import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

export interface StockDto { materialCode: string; name: string; quantityOnHand: number; reservedQuantity: number; availableQuantity: number; }
export interface ReceiveGoodsLineRequest { materialCode: string; quantity: number; unitPrice: number; }
export interface ReceiveGoodsRequest { supplierName: string; invoiceNumber: string; invoiceDate: string; lines: ReceiveGoodsLineRequest[]; }

export const inventoryApi = {
  list: (params: { search?: string; limit?: number; offset?: number }): Promise<ApiPage<StockDto>> =>
    apiRequestPaged<StockDto>(`/api/inventory${queryString(params)}`),
  getByMaterialCode: (code: string) => apiRequest<StockDto>(`/api/inventory/${code}`),
  adjust: (payload: { materialCode: string; delta: number }) =>
    apiRequest<StockDto>('/api/inventory/adjust', { method: 'POST', body: JSON.stringify(payload) }),
  receive: (payload: ReceiveGoodsRequest) =>
    apiRequest<void>('/api/inventory/receipt', { method: 'POST', body: JSON.stringify(payload) }),
}
