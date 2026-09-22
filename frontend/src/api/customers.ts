import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

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

export const customersApi = {
  /** The first page, for the places that only need something to pick from. */
  list: () => apiRequest<Customer[]>('/api/customers'),
  page: (params: { search?: string; type?: string; active?: boolean; limit?: number; offset?: number }): Promise<ApiPage<Customer>> =>
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
