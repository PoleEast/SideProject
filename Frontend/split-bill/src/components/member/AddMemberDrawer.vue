<script setup lang="ts">
import { ref, useTemplateRef, watch } from 'vue'

import { Check, X } from '@lucide/vue'

import { useRegle } from '@regle/core'
import { maxLength, required, withMessage } from '@regle/rules'

import { addGroupMembers } from '@/api/groupMember'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'

const open = defineModel<boolean>('open', { required: true })
const { groupId } = defineProps<{
  groupId: number
}>()
const emit = defineEmits<{
  added: []
}>()

const formData = ref({ displayName: '' })
const isSubmitting = ref(false)
const errorMessage = ref('')
const addedNames = ref<string[]>([])
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

const handleSubmit = async () => {
  const { valid, data } = await r$.$validate()
  if (!valid) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''
  const result = await addGroupMembers(groupId, { displayNames: [data.displayName.trim()] })
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  addedNames.value.push(...result.data.map((member) => member.displayName))
  r$.$reset({ toOriginalState: true })
  // 按「加入」時焦點會跑到按鈕上，拉回輸入框才能接著打下一位
  nameInput.value?.$el.focus()
  emit('added')
}

watch(open, (isOpen) => {
  if (!isOpen) {
    return
  }
  r$.$reset({ toOriginalState: true })
  errorMessage.value = ''
  addedNames.value = []
})
</script>

<template>
  <Drawer :open="open" @update:open="handleOpenChange">
    <DrawerContent>
      <DrawerHeader class="flex-row items-start gap-2">
        <div class="flex flex-1 flex-col gap-0.5 pt-1.5">
          <DrawerTitle>新增成員</DrawerTitle>
          <DrawerDescription>只要填名字，朋友之後點邀請連結就能認領</DrawerDescription>
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
      <form class="flex flex-col gap-4 px-5 pt-5 pb-7" @submit.prevent="handleSubmit">
        <Field :data-invalid="r$.displayName.$error || errorMessage !== ''">
          <FieldLabel for="member-name">名字</FieldLabel>
          <div class="flex gap-2">
            <Input
              id="member-name"
              ref="nameInput"
              v-model="formData.displayName"
              type="text"
              :aria-invalid="r$.displayName.$error || errorMessage !== ''"
              autocomplete="off"
              placeholder="輸入名字"
              @update:model-value="clearErrors"
            />
            <Button type="submit" size="md" class="w-19 shrink-0" :disabled="isSubmitting">
              <Spinner v-if="isSubmitting" />
              <span v-else>加入</span>
            </Button>
          </div>
          <FieldDescription>按 Enter 也能加入，加完可以接著打下一位</FieldDescription>
          <FieldError :errors="errorMessage ? [errorMessage] : r$.displayName.$errors" />
        </Field>
        <div v-if="addedNames.length > 0" class="flex flex-col gap-2">
          <p class="text-sm font-semibold">
            剛剛加入
            <span class="text-muted-foreground font-normal tabular-nums">{{
              addedNames.length
            }}</span>
          </p>
          <ul class="flex flex-wrap gap-2">
            <li v-for="name in addedNames" :key="name">
              <Badge variant="soft" size="lg">
                <Check aria-hidden="true" />
                {{ name }}
              </Badge>
            </li>
          </ul>
        </div>
      </form>
    </DrawerContent>
  </Drawer>
</template>
