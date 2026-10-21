<script setup lang="ts">
import { ref, watch } from 'vue'

import { resetInviteCode } from '@/api/group'
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

const open = defineModel<boolean>('open', { required: true })
const { groupId } = defineProps<{
  groupId: number
}>()
const emit = defineEmits<{
  reset: []
}>()

const isSubmitting = ref(false)
const errorMessage = ref('')

const handleOpenChange = (value: boolean) => {
  if (isSubmitting.value) {
    return
  }
  open.value = value
}

const confirm = async () => {
  isSubmitting.value = true
  errorMessage.value = ''
  const result = await resetInviteCode(groupId)
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  open.value = false
  emit('reset')
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
        <DialogTitle>重置邀請連結？</DialogTitle>
        <DialogDescription>舊的連結會馬上失效，還沒加入的朋友要用新的連結。</DialogDescription>
      </DialogHeader>
      <p v-if="errorMessage" class="text-destructive text-sm">{{ errorMessage }}</p>
      <DialogFooter>
        <DialogClose as-child>
          <Button type="button" size="md" variant="outline" :disabled="isSubmitting"> 取消 </Button>
        </DialogClose>
        <Button type="button" size="md" :disabled="isSubmitting" @click="confirm">
          <Spinner v-if="isSubmitting" />
          重置
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
