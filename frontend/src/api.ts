export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  operationId?: string
}

export type AuthResult = {
  userId: string
  name: string
  email: string
  token: string
  isApproved: boolean
}

export type WorkOrder = {
  id: string
  customerId: string
  assignedUserId?: string
  number: string
  title: string
  total: number
  status: string
  signatureData?: string
  proofOfWorkPhotoUrl?: string
  checkInTime?: string
  checkOutTime?: string
}

export type Customer = { id: string; fullName: string; email: string; phone: string; isActive: boolean; createdAt: string }
export type Quote = { 
  id: string
  customerId: string
  number: string
  title: string
  total: number
  state: string
  rejectionReason?: string
  requiredDepositPercentage: number
  depositPaidAmount: number
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

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
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

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
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
  list: () => apiRequest<Customer[]>('/api/customers'),
  create: (payload: { fullName: string; email: string; phone: string }) => apiRequest<Customer>('/api/customers', { method: 'POST', body: JSON.stringify(payload) }),
}

export const quotesApi = {
  list: (customerId?: string) => apiRequest<Quote[]>(`/api/quotes${customerId ? `?customerId=${customerId}` : ''}`),
  create: (payload: { customerId: string; title: string }) => apiRequest<Quote>('/api/quotes', { method: 'POST', body: JSON.stringify(payload) }),
  issue: (id: string) =>
    apiRequest<Quote>(`/api/quotes/${id}/issue`, { method: 'POST' }),
  accept: (id: string, requiredDepositPercentage?: number) =>
    apiRequest<Quote>(`/api/quotes/${id}/accept`, { 
      method: 'POST',
      body: JSON.stringify({ requiredDepositPercentage })
    }),
  payDeposit: (id: string, amount: number) =>
    apiRequest<Quote>(`/api/quotes/${id}/pay-deposit`, {
      method: 'POST',
      body: JSON.stringify({ amount })
    }),
  reject: (id: string, reason: string) =>
    apiRequest<Quote>(`/api/quotes/${id}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    }),
}

export const workOrdersApi = {
  list: () => apiRequest<WorkOrder[]>('/api/workorders'),
  getById: (id: string) => apiRequest<WorkOrder>(`/api/workorders/${id}`),
  create: (payload: { customerId: string; title: string }) =>
    apiRequest<WorkOrder>('/api/workorders', { method: 'POST', body: JSON.stringify(payload) }),
  assign: (id: string, employeeUserId: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/assign`, {
      method: 'POST',
      body: JSON.stringify({ employeeUserId }),
    }),
  complete: (id: string, signatureData?: string, proofOfWorkPhotoUrl?: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify({ signatureData, proofOfWorkPhotoUrl }),
    }),
  addItem: (id: string, description: string, quantity: number, unitPrice: number) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/items`, {
      method: 'POST',
      body: JSON.stringify({ description, quantity, unitPrice }),
    }),
  checkIn: (id: string) =>
    apiRequest<WorkOrder>(`/api/workorders/${id}/check-in`, {
      method: 'POST',
    }),
}
export interface StockDto { materialCode: string; name: string; quantityOnHand: number; reservedQuantity: number; availableQuantity: number; }
export interface PaymentDto { id: string; customerId: string; amount: number; paymentMethod: string; paymentDate: string; }
export interface AuditLogDto { id: string; userId: string; action: string; entityName: string; entityId: string; details: string; timestamp: string; }
export const inventoryApi = { getByMaterialCode: (code: string) => apiRequest<StockDto>(`/api/inventory/${code}`), adjust: (payload: { materialCode: string; delta: number }) => apiRequest<StockDto>('/api/inventory/adjust', { method: 'POST', body: JSON.stringify(payload) }) };
export const paymentsApi = { listByCustomer: (customerId: string) => apiRequest<PaymentDto[]>(`/api/payments/${customerId}`), create: (payload: { customerId: string; amount: number; paymentMethod: string }) => apiRequest<PaymentDto>('/api/payments', { method: 'POST', body: JSON.stringify(payload) }) };
export const auditLogsApi = { listRecent: () => apiRequest<AuditLogDto[]>('/api/audit-logs/recent') };
