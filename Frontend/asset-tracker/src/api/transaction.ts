import type { TransactionRequest, TransactionResponse } from '@/types/transaction'
import { authFetch } from './helper'
import type { Result, ValidationErrorResponse } from '@/types/apiResponse'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/transaction`

// 400 有兩種格式：欄位驗證是帶 errors 的 JSON，業務規則是純文字訊息
const readBadRequestMessage = async (response: Response): Promise<string> => {
  const fallback = '請確認輸入內容'

  if (!response.headers.get('content-type')?.includes('json')) {
    return (await response.text()) || fallback
  }

  const body: ValidationErrorResponse = await response.json()
  return Object.values(body.errors ?? {})[0]?.[0] ?? fallback
}

export const create = async (data: TransactionRequest): Promise<Result<TransactionResponse>> => {
  const response = await authFetch(`${API_URL}`, {
    method: 'POST',
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 400:
        message = await readBadRequestMessage(response)
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}

export const getTransactions = async (): Promise<Result<TransactionResponse[]>> => {
  const response = await authFetch(`${API_URL}`, {
    method: 'GET',
  })

  if (!response.ok) {
    return { ok: false, message: '伺服器發生無法預期狀況' }
  }

  return { ok: true, data: await response.json() }
}

export const updateTransaction = async (
  id: number,
  data: TransactionRequest,
): Promise<Result<TransactionResponse>> => {
  const response = await authFetch(`${API_URL}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 400:
        message = await readBadRequestMessage(response)
        break
      case 404:
        message = '這筆交易已不存在'
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}

export const deleteTransaction = async (id: number): Promise<Result<null>> => {
  const response = await authFetch(`${API_URL}/${id}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 400:
        message = await readBadRequestMessage(response)
        break
      case 404:
        message = '這筆交易已不存在'
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: null }
}
