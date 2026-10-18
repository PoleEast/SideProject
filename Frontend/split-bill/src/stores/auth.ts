import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { getMe } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const me = ref<string>('')

  const isLoggedIn = computed(() => {
    return token.value != null
  })

  const setToken = (loginToken: string) => {
    token.value = loginToken
    localStorage.setItem('token', loginToken)
  }

  const logout = () => {
    token.value = null
    me.value = ''
    localStorage.removeItem('token')
  }

  const loadMe = async () => {
    const result = await getMe()
    if (result.ok) {
      me.value = result.data.name
    }
  }

  return { token, me, isLoggedIn, logout, setToken, loadMe }
})
