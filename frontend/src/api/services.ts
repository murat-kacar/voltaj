import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

export type ServiceItem = {
  id: string
  description: string
  quantity: number
  unit: string
  unitPrice: number
  vatRate: number
  kind: string
  lineTotal: number
  isActive: boolean
  addedAt: string
  removedAt?: string | null
  auditNote: string
}

export type ServiceBilling = {
  currentTotal: number
  totalBilled: number
  remainingLimit: number
  depositPaidAmount: number
  requiredDepositPercentage: number
  requiredDepositAmount: number
}

export type ServiceStatus = 'Draft' | 'Quoted' | 'Accepted' | 'InProgress' | 'Completed' | 'Invoiced' | 'Cancelled' | 'Rejected' | 'OnHold' | 'Active' | 'Issued'
export type ServiceSubStatus = 'None' | 'WaitingForInternalApproval' | 'AwaitingCustomerResponse' | 'InRevision' | 'WaitingForMaterials' | 'Scheduled' | 'InTransit' | 'OnSite' | 'WorkInProgress' | 'QualityCheck' | 'PendingCustomerApproval' | 'WaitingForAccess' | 'WaitingForPayment' | 'WeatherDelay' | 'Invoiced' | 'Paid'

export type ServiceDto = {
  id: string
  customerId: string
  number: string
  title: string
  notes?: string | null
  status: ServiceStatus
  subStatus: ServiceSubStatus
  assignedUserId?: string | null
  siteId?: string | null
  assetId?: string | null
  
  validUntil?: string | null
  issuedAt?: string | null
  decidedAt?: string | null
  rejectionReason?: string | null
  isChangeOrder: boolean
  parentServiceId?: string | null

  // Maintenance Contract Support
  isMaintenanceContract?: boolean
  maintenancePeriod?: 'Monthly' | 'Quarterly' | 'Biannually' | 'Yearly' | null
  nextMaintenanceDate?: string | null

  billing: ServiceBilling
  items: ServiceItem[]
  createdAt: string
  updatedAt?: string | null
}

export type ServiceSummaryDto = {
  id: string
  customerId: string
  number: string
  title: string
  status: ServiceStatus
  subStatus: ServiceSubStatus
  assignedUserId?: string | null
  currentTotal: number
  remainingLimit: number
  createdAt: string
  isMaintenanceContract?: boolean
  maintenancePeriod?: 'Monthly' | 'Quarterly' | 'Biannually' | 'Yearly' | null
  nextMaintenanceDate?: string | null
}

export type ServiceLineRequest = {
  description: string
  quantity: number
  unitPrice: number
  unit?: string | null
  vatRate?: number | null
  kind?: string | null
  auditNote?: string | null
}

export type CreateServiceDraftRequest = {
  customerId: string
  title: string
  notes?: string | null
  validUntil?: string | null
  siteId?: string | null
  assetId?: string | null
  items?: ServiceLineRequest[] | null
  isMaintenanceContract?: boolean
  maintenancePeriod?: 'Monthly' | 'Quarterly' | 'Biannually' | 'Yearly' | null
  nextMaintenanceDate?: string | null
}

export type UpdateServiceDraftRequest = {
  title: string
  notes?: string | null
  validUntil?: string | null
  siteId?: string | null
  assetId?: string | null
  items?: ServiceLineRequest[] | null
}

export const servicesApi = {
  list: (params?: { search?: string; status?: string; customerId?: string; assignedUserId?: string; limit?: number; offset?: number }): Promise<ApiPage<ServiceSummaryDto>> =>
    apiRequestPaged<ServiceSummaryDto>(`/api/services${queryString(params ?? {})}`),
  get: (id: string) => apiRequest<ServiceDto>(`/api/services/${id}`),
  createDraft: (payload: CreateServiceDraftRequest) => apiRequest<ServiceDto>('/api/services', { method: 'POST', body: JSON.stringify(payload) }),
  updateDraft: (id: string, payload: UpdateServiceDraftRequest) => apiRequest<ServiceDto>(`/api/services/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  
  addItem: (id: string, payload: ServiceLineRequest) => apiRequest<ServiceDto>(`/api/services/${id}/items`, { method: 'POST', body: JSON.stringify(payload) }),
  removeItem: (id: string, itemId: string, auditNote?: string) => apiRequest<ServiceDto>(`/api/services/${id}/items/${itemId}${queryString({ auditNote })}`, { method: 'DELETE' }),
  
  issue: (id: string) => apiRequest<ServiceDto>(`/api/services/${id}/issue`, { method: 'POST' }),
  accept: (id: string, requiredDepositPercentage?: number) => apiRequest<ServiceDto>(`/api/services/${id}/accept`, { method: 'POST', body: JSON.stringify({ requiredDepositPercentage }) }),
  reject: (id: string, reason: string) => apiRequest<ServiceDto>(`/api/services/${id}/reject`, { method: 'POST', body: JSON.stringify({ reason }) }),
  deposit: (id: string, amount: number, paymentMethod: string) => apiRequest<ServiceDto>(`/api/services/${id}/deposit`, { method: 'POST', body: JSON.stringify({ amount, paymentMethod }) }),
  cancel: (id: string, reason?: string) => apiRequest<ServiceDto>(`/api/services/${id}/cancel`, { method: 'POST', body: JSON.stringify({ reason }) }),
  
  assign: (id: string, assignedUserId: string) => apiRequest<ServiceDto>(`/api/services/${id}/assign`, { method: 'POST', body: JSON.stringify({ assignedUserId }) }),
  hold: (id: string, reason: string) => apiRequest<ServiceDto>(`/api/services/${id}/hold`, { method: 'POST', body: JSON.stringify({ reason }) }),
  resume: (id: string) => apiRequest<ServiceDto>(`/api/services/${id}/resume`, { method: 'POST' }),
  complete: (id: string, finalAmountOverride?: number) => apiRequest<ServiceDto>(`/api/services/${id}/complete`, { method: 'POST', body: JSON.stringify({ finalAmountOverride }) }),
  updateSubStatus: (id: string, subStatus: ServiceSubStatus) => apiRequest<ServiceDto>(`/api/services/${id}/substatus`, { method: 'POST', body: JSON.stringify({ subStatus }) }),
  
  partialInvoice: (id: string, amount: number) => apiRequest<ServiceDto>(`/api/services/${id}/partial-invoice`, { method: 'POST', body: JSON.stringify({ amount }) }),
}
