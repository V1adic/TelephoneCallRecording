export interface ApiErrorShape {
  code: string
  message: string
}

export class ApiError extends Error {
  status: number
  code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

export interface Profile {
  username: string
  email: string
  isEmailConfirmed: boolean
  phoneNumber: string | null
  city: string | null
}

export interface AuthMessageResponse {
  code: string
  message: string
}

export interface CsrfResponse {
  token: string
}

export interface RegisterPayload {
  username: string
  password: string
  email: string
  phoneNumber: string
  inn: string
  address: string
  cityId: number
}

export interface LoginPayload {
  username: string
  password: string
}

export interface CityOption {
  id: number
  name: string
}

export interface ActiveCall {
  destPhone: string
  startedAtUtc: string
  timeOfDay: string
}

export interface ActiveCallEnvelope {
  activeCall: ActiveCall | null
}

export interface CallStartResult {
  success: boolean
  code: string
  message: string
  destPhone: string | null
  startedAtUtc: string | null
  timeOfDay: string | null
}

export interface CallEndResult {
  success: boolean
  code: string
  message: string
  cost: number
  discountPercent: number
  baseCost: number
  durationMinutes: number
  destPhone: string | null
  timeOfDay: string | null
}

let csrfToken: string | null = null

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  const method = (init.method ?? 'GET').toUpperCase()
  const hasBody = init.body !== undefined

  headers.set('Accept', 'application/json')
  if (hasBody) {
    headers.set('Content-Type', 'application/json')
  }

  if (method !== 'GET' && method !== 'HEAD' && csrfToken) {
    headers.set('X-CSRF-TOKEN', csrfToken)
  }

  const response = await fetch(path, {
    ...init,
    method,
    headers,
    credentials: 'include'
  })

  if (!response.ok) {
    const parsedError = await tryReadError(response)
    throw new ApiError(response.status, parsedError.code, parsedError.message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function tryReadError(response: Response): Promise<ApiErrorShape> {
  try {
    return (await response.json()) as ApiErrorShape
  } catch {
    return {
      code: 'unknown_error',
      message: 'Не удалось обработать ответ сервера.'
    }
  }
}

export async function getCsrfToken() {
  const response = await request<CsrfResponse>('/auth/csrf')
  csrfToken = response.token
  return response.token
}

export function clearCsrfToken() {
  csrfToken = null
}

export async function login(payload: LoginPayload) {
  return request<Profile>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function register(payload: RegisterPayload) {
  return request<AuthMessageResponse>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function requestConfirmation() {
  return request<AuthMessageResponse>('/auth/request-confirmation', {
    method: 'POST',
    body: JSON.stringify({})
  })
}

export async function confirmEmail(code: string) {
  return request<AuthMessageResponse>('/auth/confirm-email', {
    method: 'POST',
    body: JSON.stringify({ code })
  })
}

export async function fetchProfile() {
  return request<Profile>('/auth/profile')
}

export async function logout() {
  return request<AuthMessageResponse>('/auth/logout', {
    method: 'POST',
    body: JSON.stringify({})
  })
}

export async function fetchCities() {
  return request<CityOption[]>('/calls/cities')
}

export async function fetchActiveCall() {
  return request<ActiveCallEnvelope>('/calls/active')
}

export async function startCall(destPhone: string) {
  return request<CallStartResult>('/calls/start', {
    method: 'POST',
    body: JSON.stringify({ destPhone })
  })
}

export async function endCall(destPhone: string) {
  return request<CallEndResult>('/calls/end', {
    method: 'POST',
    body: JSON.stringify({ destPhone })
  })
}

export async function fetchHealth() {
  return request<{ status: string; database: string }>('/health')
}
