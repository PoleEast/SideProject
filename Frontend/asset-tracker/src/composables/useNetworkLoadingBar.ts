import { useNetworkStore } from '@/stores/network'
import { useLoadingBar } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { watch } from 'vue'

export const useNetworkLoadingBar = () => {
  const loadingBar = useLoadingBar()
  const { isLoading } = storeToRefs(useNetworkStore())

  watch(isLoading, (loading) => {
    if (loading) loadingBar.start()
    else loadingBar.finish()
  })

  return { isLoading }
}
