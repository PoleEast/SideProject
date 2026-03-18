import type { Result, ValidationErrorResponse } from '@/types/apiResponse'
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/types/auth'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/auth`

export const login = async (data: LoginRequest): Promise<Result<AuthResponse>> => {
  const response = await fetch(`${API_URL}/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '帳號或密碼錯誤'
        break
      case 400:
        const body: ValidationErrorResponse = await response.json()
        message = Object.values(body.errors)?.[0]?.[0] ?? '請確認輸入內容'
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}

export const register = async (data: RegisterRequest): Promise<Result<AuthResponse>> => {
  const response = await fetch(`${API_URL}/register`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 409:
        message = '此帳號已註冊過'
        break
      case 400:
        const body: ValidationErrorResponse = await response.json()
        message = Object.values(body.errors)?.[0]?.[0] ?? '請確認輸入內容'
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}
