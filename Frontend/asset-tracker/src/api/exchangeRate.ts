import type { Result } from '@/types/apiResponse'
import type { CurrencyType } from '@/types/common'
import type { ExchangeRateResponse } from '@/types/exchangeRate'
import { authFetch } from './helper'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/exchangeRate`

export const getExchangeRate = async (
  currencyType: CurrencyType,
): Promise<Result<ExchangeRateResponse>> => {
  const response = await authFetch(`${API_URL}/${currencyType}`, {
    method: 'GET',
  })

  if (!response.ok) {
    return { ok: false, message: '伺服器發生無法預期狀況' }
  }

  return { ok: true, data: await response.json() }
}
