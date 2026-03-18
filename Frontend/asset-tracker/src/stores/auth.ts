import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))

  const isLoggedIn = computed(() => {
    return token.value != null
  })

  const setToken = (loginToken: string) => {
    token.value = loginToken
    localStorage.setItem('token', loginToken)
  }

  const logout = () => {
    token.value = null
    localStorage.removeItem('token')
  }

  return { token, isLoggedIn, logout, setToken }
})
