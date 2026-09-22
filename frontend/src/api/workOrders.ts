import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

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

export const workOrdersApi = {
  list: (params?: { limit?: number; offset?: number }): Promise<ApiPage<WorkOrder>> =>
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
