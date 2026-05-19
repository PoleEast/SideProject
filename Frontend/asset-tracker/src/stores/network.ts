import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export const useNetworkStore = defineStore('network', () => {
  const pendingCount = ref(0)
  const isLoading = computed(() => pendingCount.value > 0)

  const start = () => pendingCount.value++
  const finish = () => {
    if (pendingCount.value > 0) {
      pendingCount.value--
    }
  }

  return {
    isLoading,
    start,
    finish,
  }
})
