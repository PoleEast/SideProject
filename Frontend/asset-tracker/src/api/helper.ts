import { useAuthStore } from '@/stores/auth'
import { useNetworkStore } from '@/stores/network'

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
