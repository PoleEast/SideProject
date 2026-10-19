import { onMounted, ref } from 'vue'

import { getMyGroups } from '@/api/group'
import type { GroupListItemResponse } from '@/types/group'

export const useMyGroups = () => {
  const groups = ref<GroupListItemResponse[]>([])
  const loadState = ref<'loading' | 'error' | 'ready'>('loading')
  const loadErrorMessage = ref<string>('')

  const loadGroups = async () => {
    loadState.value = 'loading'
    const result = await getMyGroups()

    if (!result.ok) {
      loadErrorMessage.value = result.message
      loadState.value = 'error'
      return
    }

    groups.value = result.data
    loadState.value = 'ready'
  }

  onMounted(loadGroups)

  return { loadState, loadErrorMessage, groups, loadGroups }
}
