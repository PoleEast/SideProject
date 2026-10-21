<script setup lang="ts">
import { ref, watch } from 'vue'

import { setGroupClosed } from '@/api/group'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Spinner } from '@/components/ui/spinner'
import type { GroupListItemResponse, GroupResponse } from '@/types/group'

const open = defineModel<boolean>('open', { required: true })
const { group } = defineProps<{
  group: GroupListItemResponse | null
}>()
const emit = defineEmits<{
  closed: [group: GroupResponse]
}>()

const isClosing = ref(false)
const errorMessage = ref('')

const handleOpenChange = (value: boolean) => {
  if (isClosing.value) {
    return
  }
  open.value = value
}
const confirm = async () => {
  if (!group) {
    return
  }

  isClosing.value = true
  const result = await setGroupClosed(group.id, { closed: true })
  isClosing.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  emit('closed', result.data)
  open.value = false
}

watch(open, (isOpen) => {
  if (isOpen) {
    errorMessage.value = ''
  }
})
</script>

<template>
  <Dialog :open="open" @update:open="handleOpenChange">
    <DialogContent :show-close-button="false">
      <DialogHeader>
        <DialogTitle>結束「{{ group?.name }}」？</DialogTitle>
        <DialogDescription>結束後會收進「已結束」，之後隨時可以重新開啟。</DialogDescription>
      </DialogHeader>
      <p v-if="errorMessage" class="text-destructive text-sm">{{ errorMessage }}</p>
      <DialogFooter>
        <DialogClose as-child>
          <Button type="button" size="md" variant="outline" :disabled="isClosing">取消</Button>
        </DialogClose>
        <Button type="button" size="md" :disabled="isClosing" @click="confirm">
          <Spinner v-if="isClosing" />
          {{ isClosing ? '結束中…' : '結束' }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
