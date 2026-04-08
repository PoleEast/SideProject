import { authFetch } from './helper'
import type { PositionResponse } from '@/types/Position'
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
