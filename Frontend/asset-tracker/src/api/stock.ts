import type { Result, ValidationErrorResponse } from '@/types/apiResponse'
import type { BatchStockPriceRequest, BatchStockPriceResponse } from '@/types/stock'
import { authFetch } from './helper'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/stock`

export const getLatestStockPrices = async (
  datas: BatchStockPriceRequest[],
  asOf: Date,
): Promise<Result<BatchStockPriceResponse>> => {
  const params = new URLSearchParams({ asOf: asOf.toISOString().split('T')[0]! })
  const response = await authFetch(`${API_URL}/latest-prices?${params}`, {
    method: 'POST',
    body: JSON.stringify(datas),
  })

  if (!response.ok) {
    let message
    switch (response.status) {
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
