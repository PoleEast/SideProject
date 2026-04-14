import { authFetch } from './helper'
import type { PositionResponse, RealizedPnlResponse } from '@/types/Position'
import type { Result } from '@/types/apiResponse'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/position`

export const getPosition = async (): Promise<Result<PositionResponse[]>> => {
  const response = await authFetch(`${API_URL}`, {
    method: 'GET',
  })

  if (!response.ok) {
    return { ok: false, message: '伺服器發生無法預期狀況' }
  }

  return { ok: true, data: await response.json() }
}

export const getRealizedPnl = async (): Promise<Result<RealizedPnlResponse[]>> => {
  const response = await authFetch(`${API_URL}/realized-pnl`, {
    method: 'GET',
  })

  if (!response.ok) {
    let message
    switch (response.status) {
      case 400:
        message = await response.text()
        break
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}
