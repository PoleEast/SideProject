import { authHeaders } from './helper'
import type { PositionResponse } from '@/types/Position'
import type { Result } from '@/types/apiResponse'

const BASE_URL = import.meta.env.VITE_API_URL
const API_URL = `${BASE_URL}/position`

export const getPosition = async (): Promise<Result<PositionResponse[]>> => {
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
      case 400:
        message = response.text()
      default:
        message = '伺服器發生無法預期狀況'
    }

    return { ok: false, message }
  }

  return { ok: true, data: await response.json() }
}
