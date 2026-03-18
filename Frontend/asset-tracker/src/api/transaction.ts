import type { TransactionRequest, TransactionResponse } from '@/types/transaction'
import { authHeaders } from './helper'
import type { Result, ValidationErrorResponse } from '@/types/apiResponse'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/transaction`

export const create = async (data: TransactionRequest): Promise<Result<TransactionResponse>> => {
  const response = await fetch(`${API_URL}`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '不允許的操作'
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

export const getTransactions = async (): Promise<Result<TransactionResponse[]>> => {
  const response = await fetch(`${API_URL}`, {
    method: 'GET',
    headers: authHeaders(),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '不允許的操作'
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}

export const getTransaction = async (id: number): Promise<Result<TransactionResponse>> => {
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'GET',
    headers: authHeaders(),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '不允許的操作'
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

export const updateTransaction = async (
  id: number,
  data: TransactionRequest,
): Promise<Result<TransactionResponse>> => {
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'PUT',
    headers: authHeaders(),
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '不允許的操作'
        break
      case 400:
        const body: ValidationErrorResponse = await response.json()
        message = Object.values(body.errors)?.[0]?.[0] ?? '請確認輸入內容'
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
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 401:
        message = '不允許的操作'
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
