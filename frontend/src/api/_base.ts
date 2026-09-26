// Shared HTTP primitives — used by all domain API modules.
// V2: All external input passes through a single entry point (apiFetch).

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  operationId?: string
  errorCode?: string
}

export type AuthResult = {
  userId: string
  name: string
  email: string
  token: string
  isApproved: boolean
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

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
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
      if (session) {
        localStorage.removeItem('voltflow.session')
        const publicPaths = ['/test-data', '/endpoint-trigger', '/login']
        if (!publicPaths.some((p) => window.location.pathname.startsWith(p))) {
          window.location.reload()
        }
      }
    }
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    const message = problem.detail || problem.title || `Request failed with status ${response.status}`
    throw new ApiError(message, response.status, problem)
  }

  return response
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await apiFetch(path, init)
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export type ApiPage<T> = { items: T[]; total: number }

/** A list endpoint that pages: the items come in the body, the size of the whole result in the X-Total-Count header. */
export async function apiRequestPaged<T>(path: string): Promise<ApiPage<T>> {
  const response = await apiFetch(path)
  const data = await response.json()
  
  if (Array.isArray(data)) {
    const total = Number(response.headers.get('X-Total-Count'))
    return { items: data, total: Number.isFinite(total) && total >= data.length ? total : data.length }
  }
  
  return { 
    items: data.items || [], 
    total: data.totalCount ?? data.items?.length ?? 0 
  }
}

/** Builds a query string from the parameters that have a value. */
export function queryString(params: Record<string, string | number | boolean | undefined | null>): string {
  const parts = Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== '')
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
  return parts.length > 0 ? `?${parts.join('&')}` : ''
}

/** The roles inside the signed-in user's token. The API decides what is allowed; this only lets the screen hide what would be refused. */
export function sessionRoles(): string[] {
  try {
    const session = localStorage.getItem('voltflow.session')
    const token = session ? (JSON.parse(session) as AuthResult).token : undefined
    const payload = token?.split('.')[1]
    if (!payload) return []
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const claims = JSON.parse(json) as Record<string, unknown>
    const raw = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? claims.role ?? []
    return Array.isArray(raw) ? raw.map(String) : [String(raw)]
  } catch {
    return []
  }
}
