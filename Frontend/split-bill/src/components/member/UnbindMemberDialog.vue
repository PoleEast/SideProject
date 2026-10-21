<script setup lang="ts">
import { ref, watch } from 'vue'

import { unbindGroupMember } from '@/api/groupMember'
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
import type { GroupMemberResponse } from '@/types/groupMember'

const open = defineModel<boolean>('open', { required: true })
const { groupId, member } = defineProps<{
  groupId: number
  member: GroupMemberResponse | null
}>()
const emit = defineEmits<{
  unbound: []
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
  if (!member) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''
  const result = await unbindGroupMember(groupId, member.id)
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  open.value = false
  emit('unbound')
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
        <DialogTitle>解除「{{ member?.displayName }}」的綁定？</DialogTitle>
        <DialogDescription>
          {{ member?.displayName }}會看不到這個群組，名字和帳目會留在名單上。
        </DialogDescription>
      </DialogHeader>
      <p v-if="errorMessage" class="text-destructive text-sm">{{ errorMessage }}</p>
      <DialogFooter>
        <DialogClose as-child>
          <Button type="button" size="md" variant="outline" :disabled="isSubmitting"> 取消 </Button>
        </DialogClose>
        <Button
          type="button"
          size="md"
          variant="destructive"
          :disabled="isSubmitting"
          @click="confirm"
        >
          <Spinner v-if="isSubmitting" />
          解除綁定
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
