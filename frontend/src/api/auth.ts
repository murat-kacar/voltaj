import { apiRequest, apiRequestPaged, queryString, type ApiPage } from './_base'

export type { AuthResult, ProblemDetails, ApiError, ApiPage } from './_base'
export { apiRequest, apiRequestPaged, apiFetch, queryString, sessionRoles } from './_base'

export type UserSummary = { id: string; name: string; email: string; isApproved: boolean; roles: string[] }

export const authApi = {
  login: (email: string, password: string) =>
    apiRequest<import('./_base').AuthResult>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  register: (payload: { name: string; email: string; password: string; otp?: string }) =>
    apiRequest<import('./_base').AuthResult>('/api/auth/register', {
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

export const usersApi = {
  /** `approved: false` narrows the list to the users still waiting for approval. */
  page: (params: { approved?: boolean; limit?: number; offset?: number }): Promise<ApiPage<UserSummary>> =>
    apiRequestPaged<UserSummary>(`/api/auth/users${queryString(params)}`),
  approve: (id: string) => apiRequest<unknown>(`/api/auth/users/${id}/approve`, { method: 'POST' }),
  assignRole: (id: string, roleName: string) =>
    apiRequest<void>(`/api/auth/users/${id}/roles`, { method: 'POST', body: JSON.stringify({ roleName }) }),
}
