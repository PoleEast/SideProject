import { useAuthStore } from '@/stores/auth'
import { useNetworkStore } from '@/stores/network'
import type { Result, ValidationErrorResponse } from '@/types/apiResponse'

const BASE_URL = import.meta.env.VITE_API_URL

// 400 欄位驗證是帶 errors 的 JSON，業務規則與找不到是純文字，未預期的例外是不帶 errors 的 JSON
const readErrorMessage = async (response: Response): Promise<string> => {
  const fallbackMessage = '伺服器發生無法預期狀況'
  const text = await response.text()

  if (!response.headers.get('content-type')?.includes('json')) {
    return text || fallbackMessage
  }

  const body: Partial<ValidationErrorResponse> = JSON.parse(text)
  return Object.values(body.errors ?? {})[0]?.[0] ?? fallbackMessage
}

export const authHeaders = (token: string) => {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  }
}

export class UnauthorizedError extends Error {
  constructor() {
    super('登入已過期，請重新登入')
    this.name = 'UnauthorizedError'
  }
}

export const authFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const authStore = useAuthStore()
  const networkStore = useNetworkStore()

  networkStore.start()

  try {
    const response = await fetch(url, {
      ...options,
      headers: {
        ...authHeaders(authStore.token ?? ''),
        ...options.headers,
      },
    })

    if (response.status === 401) {
      authStore.logout()
      throw new UnauthorizedError()
    }

    return response
  } finally {
    networkStore.finish()
  }
}

export const request = async <T>(
  path: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  body?: unknown,
): Promise<Result<T>> => {
  try {
    const response = await authFetch(`${BASE_URL}${path}`, {
      method: method,
      body: body === undefined ? undefined : JSON.stringify(body),
    })

    if (!response.ok) {
      return { ok: false, status: response.status, message: await readErrorMessage(response) }
    }

    const text = await response.text()
    return { ok: true, data: text ? JSON.parse(text) : null }
  } catch (error) {
    if (error instanceof UnauthorizedError) {
      return { ok: false, status: 401, message: error.message }
    }
    return { ok: false, status: 0, message: '連不上伺服器，請稍後再試' }
  }
}
