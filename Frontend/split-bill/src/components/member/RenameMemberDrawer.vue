<script setup lang="ts">
import { ref, useTemplateRef, watch } from 'vue'

import { X } from '@lucide/vue'

import { useRegle } from '@regle/core'
import { maxLength, required, withMessage } from '@regle/rules'

import { renameGroupMember } from '@/api/groupMember'
import { Button } from '@/components/ui/button'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import type { GroupMemberResponse } from '@/types/groupMember'

const open = defineModel<boolean>('open', { required: true })
const { groupId, member } = defineProps<{
  groupId: number
  member: GroupMemberResponse | null
}>()
const emit = defineEmits<{
  renamed: []
}>()

const formData = ref({ displayName: '' })
const isSubmitting = ref(false)
const errorMessage = ref('')
const nameInput = useTemplateRef('nameInput')

const { r$ } = useRegle(
  formData,
  {
    displayName: {
      required: withMessage(required, '請輸入名字'),
      maxLength: withMessage(maxLength(32), '名字最多32個字'),
    },
  },
  { autoDirty: false },
)

const clearErrors = () => {
  r$.displayName.$reset()
  errorMessage.value = ''
}

const handleOpenChange = (value: boolean) => {
  if (isSubmitting.value) {
    return
  }
  open.value = value
}

// 取代抽屜預設的聚焦，讓原本的名字整段選取，直接打字就會蓋掉
const selectNameOnOpen = (event: Event) => {
  event.preventDefault()
  const input = nameInput.value?.$el
  input?.focus()
  input?.select()
}

const handleSubmit = async () => {
  if (!member) {
    return
  }
  const { valid, data } = await r$.$validate()
  if (!valid) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''
  const result = await renameGroupMember(groupId, member.id, {
    displayName: data.displayName.trim(),
  })
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  emit('renamed')
  open.value = false
}

watch(open, (isOpen) => {
  if (!isOpen || !member) {
    return
  }
  formData.value.displayName = member.displayName
  r$.$reset()
  errorMessage.value = ''
})
</script>

<template>
  <Drawer :open="open" @update:open="handleOpenChange">
    <DrawerContent @open-auto-focus="selectNameOnOpen">
      <DrawerHeader class="flex-row items-start gap-2">
        <div class="flex flex-1 flex-col gap-0.5 pt-1.5">
          <DrawerTitle>改名</DrawerTitle>
          <DrawerDescription>大家在群組裡看到的就是這個名字</DrawerDescription>
        </div>
        <DrawerClose as-child>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            class="text-muted-foreground size-11 rounded-full"
            aria-label="關閉"
            :disabled="isSubmitting"
          >
            <X class="size-5.5" aria-hidden="true" />
          </Button>
        </DrawerClose>
      </DrawerHeader>
      <form class="flex flex-col" @submit.prevent="handleSubmit">
        <div class="p-5">
          <Field :data-invalid="r$.displayName.$error || errorMessage !== ''">
            <FieldLabel for="rename-member-name">名字</FieldLabel>
            <Input
              id="rename-member-name"
              ref="nameInput"
              v-model="formData.displayName"
              type="text"
              :aria-invalid="r$.displayName.$error || errorMessage !== ''"
              autocomplete="off"
              @update:model-value="clearErrors"
            />
            <FieldDescription>群組動態會記下改名前後的名字</FieldDescription>
            <FieldError :errors="errorMessage ? [errorMessage] : r$.displayName.$errors" />
          </Field>
        </div>
        <DrawerFooter>
          <Button type="submit" size="lg" :disabled="isSubmitting">
            <Spinner v-if="isSubmitting" />
            {{ isSubmitting ? '儲存中…' : '儲存' }}
          </Button>
        </DrawerFooter>
      </form>
    </DrawerContent>
  </Drawer>
</template>
