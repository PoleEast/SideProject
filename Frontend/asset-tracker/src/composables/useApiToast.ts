import { UnauthorizedError } from '@/api/helper'
import type { Result } from '@/types/apiResponse'
import { useNotification } from 'naive-ui'

export const useApiToast = () => {
  const notification = useNotification()

  const handle = async <T>(promise: Promise<Result<T>>): Promise<Result<T>> => {
    try {
      const result = await promise

      if (!result.ok) {
        notification.error({
          title: '操作失敗',
          content: result.message,
          duration: 4000,
        })
        return result
      }

      return result
    } catch (error) {
      if (error instanceof UnauthorizedError) {
        notification.warning({
          title: '登入已過期',
          content: '請重新登入',
          duration: 4000,
        })
        return { ok: false, message: '登入已過期' }
      }
      notification.error({
        title: '網路連線發生問題，請稍後再試',
        closable: false,
        duration: 4000,
      })
      return { ok: false, message: '網路連線發生問題' }
    }
  }

  return { handle }
}
