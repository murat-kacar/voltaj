import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

export interface AuditLogDto { id: string; userId: string; action: string; entityName: string; entityId: string; details: string; timestamp: string; }

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

export const auditLogsApi = {
  listRecent: () => apiRequest<AuditLogDto[]>('/api/audit-logs/recent'),
}

export const remindersApi = {
  list: (params?: { state?: string; limit?: number; offset?: number }): Promise<ApiPage<ReminderDto>> =>
    apiRequestPaged<ReminderDto>(`/api/reminders${queryString(params ?? {})}`),
  create: (payload: { type: string; entityName: string; entityId: string; dueAt: string; message: string }) =>
    apiRequest<ReminderDto>('/api/reminders', { method: 'POST', body: JSON.stringify(payload) }),
  dismiss: (id: string, note?: string) =>
    apiRequest<ReminderDto>(`/api/reminders/${id}/dismiss`, { method: 'POST', body: JSON.stringify({ note }) }),
  complete: (id: string, note?: string) =>
    apiRequest<ReminderDto>(`/api/reminders/${id}/complete`, { method: 'POST', body: JSON.stringify({ note }) }),
}

export const testDataApi = {
  getCounts: () => apiRequest<Record<string, number>>('/api/test-data/tables'),
  insert: (tableName: string, data: Record<string, unknown>) =>
    apiRequest<{ success: boolean; id?: string; message?: string }>('/api/test-data/insert', {
      method: 'POST',
      body: JSON.stringify({ tableName, data }),
    }),
}
