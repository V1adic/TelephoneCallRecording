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

export type UserRole = 'Admin' | 'Client'
export type AccessState = 'active' | 'locked' | 'revoked'

export interface Profile {
  username: string
  email: string
  role: UserRole
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

export interface CallHistoryItem {
  callId: number
  destPhone: string
  destinationCity: string
  startedAtUtc: string
  durationMinutes: number
  timeOfDay: string
  appliedTariff: number
  discountPercent: number
  baseCost: number
  finalCost: number
}

export interface CallSummary {
  fromUtc: string
  toUtc: string
  totalCalls: number
  totalMinutes: number
  baseCost: number
  totalCost: number
}

export interface AdminUser {
  id: number
  username: string
  email: string
  role: UserRole
  isEmailConfirmed: boolean
  subscriberId: number | null
  phone: string | null
  inn: string | null
  address: string | null
  cityId: number | null
  cityName: string | null
  lockoutEndUtc: string | null
  accessState: AccessState
}

export interface AdminCity {
  id: number
  name: string
  dayTariff: number
  nightTariff: number
  subscribersCount: number
  callsCount: number
}

export interface CityDiscount {
  id: number
  cityId: number
  minMinutes: number
  maxMinutes: number | null
  discountPercent: number
}

export interface CallDetailReport {
  callId: number
  sourcePhone: string
  destPhone: string
  destinationCity: string
  startedAtUtc: string
  durationMinutes: number
  timeOfDay: string
  appliedTariff: number
  discountPercent: number
  baseCost: number
  finalCost: number
}

export interface CityCostReport {
  name: string
  totalCalls: number
  totalMinutes: number
  totalCost: number
}

export interface SubscriberCostReport {
  phoneNumber: string
  totalCalls: number
  totalMinutes: number
  totalCost: number
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
    credentials: 'include',
    redirect: 'manual',
    cache: 'no-cache'
  })

  if (response.status >= 300 && response.status < 400) {
    throw new ApiError(401, 'unauthenticated', 'Сессия истекла.')
  }

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

function withPeriod(fromUtc?: string, toUtc?: string) {
  const params = new URLSearchParams()
  if (fromUtc) {
    params.set('from', fromUtc)
  }
  if (toUtc) {
    params.set('to', toUtc)
  }

  const query = params.toString()
  return query ? `?${query}` : ''
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

export async function fetchCallHistory(fromUtc?: string, toUtc?: string) {
  return request<CallHistoryItem[]>(`/calls/history${withPeriod(fromUtc, toUtc)}`)
}

export async function fetchCallSummary(fromUtc?: string, toUtc?: string) {
  return request<CallSummary>(`/calls/summary${withPeriod(fromUtc, toUtc)}`)
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

export async function fetchAdminUsers() {
  return request<AdminUser[]>('/admin/users')
}

export async function changeUserRole(id: number, role: UserRole) {
  return request<AuthMessageResponse>(`/admin/users/${id}/role`, {
    method: 'PATCH',
    body: JSON.stringify({ role })
  })
}

export async function updateSubscriber(id: number, payload: Pick<AdminUser, 'phone' | 'inn' | 'address' | 'cityId'>) {
  return request<AdminUser>(`/admin/users/${id}/subscriber`, {
    method: 'PUT',
    body: JSON.stringify({
      phoneNumber: payload.phone,
      inn: payload.inn,
      address: payload.address,
      cityId: payload.cityId
    })
  })
}

export async function revokeUserAccess(id: number) {
  return request<AuthMessageResponse>(`/admin/users/${id}/revoke`, {
    method: 'POST',
    body: JSON.stringify({})
  })
}

export async function restoreUserAccess(id: number) {
  return request<AuthMessageResponse>(`/admin/users/${id}/restore`, {
    method: 'POST',
    body: JSON.stringify({})
  })
}

export async function fetchAdminCities() {
  return request<AdminCity[]>('/admin/cities')
}

export async function createCity(name: string, dayTariff: number, nightTariff: number) {
  return request<AdminCity>('/admin/cities', {
    method: 'POST',
    body: JSON.stringify({ name, dayTariff, nightTariff })
  })
}

export async function updateCity(id: number, name: string, dayTariff: number, nightTariff: number) {
  return request<AdminCity>(`/admin/cities/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name, dayTariff, nightTariff })
  })
}

export async function deleteCity(id: number) {
  return request<void>(`/admin/cities/${id}`, { method: 'DELETE' })
}

export async function createDiscount(cityId: number, minMinutes: number, maxMinutes: number | null, discountPercent: number) {
  return request<CityDiscount>('/admin/discounts', {
    method: 'POST',
    body: JSON.stringify({ cityId, minMinutes, maxMinutes, discountPercent })
  })
}

export async function getDiscounts(cityId: number) {
  return request<CityDiscount[]>(`/admin/discounts/${cityId}`)
}

export async function deleteDiscount(id: number) {
  return request<void>(`/admin/discounts/${id}`, { method: 'DELETE' })
}

export async function fetchCallDetailsReport(fromUtc: string, toUtc: string) {
  return request<CallDetailReport[]>(`/reports/calls${withPeriod(fromUtc, toUtc)}`)
}

export async function fetchCityCostsReport(fromUtc: string, toUtc: string) {
  return request<CityCostReport[]>(`/reports/cities${withPeriod(fromUtc, toUtc)}`)
}

export async function fetchSubscriberCostsReport(phoneNumber: string, fromUtc: string, toUtc: string) {
  const encodedPhone = encodeURIComponent(phoneNumber)
  return request<SubscriberCostReport[]>(`/reports/subscriber/${encodedPhone}${withPeriod(fromUtc, toUtc)}`)
}
