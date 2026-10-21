<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'

import { ArrowRightLeft } from '@lucide/vue'

import { removeGroupMember, unbindGroupMember } from '@/api/groupMember'
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
const { groupId, groupName, member, counterpartNames } = defineProps<{
  groupId: number
  groupName: string
  member: GroupMemberResponse | null
  // 還跟這位成員有帳沒清的人，空陣列代表帳清了
  counterpartNames: string[]
}>()
const emit = defineEmits<{
  removed: []
  left: []
}>()

const isSubmitting = ref(false)
const errorMessage = ref('')

interface DescriptionPart {
  text: string
  isName: boolean
}

const isUnsettled = computed(() => counterpartNames.length > 0)
const isBlocked = computed(() => !member?.isMe && isUnsettled.value)

const title = computed(() => {
  if (!member) {
    return ''
  }
  if (member.isMe) {
    return `退出「${groupName}」？`
  }
  return isBlocked.value ? `還不能移除「${member.displayName}」` : `移除「${member.displayName}」？`
})

const withCounterpartNames = (before: string, names: string[], after: string) => {
  const parts: DescriptionPart[] = [{ text: before, isName: false }]
  names.forEach((name, index) => {
    if (index > 0) {
      parts.push({ text: '、', isName: false })
    }
    parts.push({ text: name, isName: true })
  })
  parts.push({ text: after, isName: false })
  return parts
}

const descriptionParts = computed<DescriptionPart[]>(() => {
  if (!member) {
    return []
  }
  if (member.isMe && isUnsettled.value) {
    return withCounterpartNames(
      '你還跟',
      counterpartNames,
      '有帳沒清，所以你的名字和帳目會留在名單上，大家照樣能結算，但退出後你就看不到這個群組了。',
    )
  }
  if (member.isMe) {
    return [
      {
        text: '退出後你就看不到這個群組了，邀請連結也會換新，要回來得請朋友傳新的連結。',
        isName: false,
      },
    ]
  }
  if (isBlocked.value) {
    return withCounterpartNames(
      `${member.displayName}還跟`,
      counterpartNames,
      '有帳沒清，結清之後才能移除。',
    )
  }
  return [
    {
      text: member.isBound
        ? `${member.displayName}會看不到這個群組，邀請連結也會換新。記過的帳都會保留。`
        : '記過的帳都會保留。',
      isName: false,
    },
  ]
})

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
  // 自己還有帳沒清就改成解除綁定：位置和帳目留在名單上，其他人照樣能結算
  const result =
    member.isMe && isUnsettled.value
      ? await unbindGroupMember(groupId, member.id)
      : await removeGroupMember(groupId, member.id)
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  open.value = false
  if (member.isMe) {
    emit('left')
  } else {
    emit('removed')
  }
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
        <DialogTitle>{{ title }}</DialogTitle>
        <DialogDescription class="leading-relaxed">
          <template v-for="(part, index) in descriptionParts" :key="index">
            <strong
              v-if="part.isName"
              class="bg-brand/40 text-foreground mx-0.5 rounded-sm px-1.5 py-0.5 font-bold"
            >
              {{ part.text }}
            </strong>
            <template v-else>{{ part.text }}</template>
          </template>
        </DialogDescription>
      </DialogHeader>
      <p v-if="errorMessage" class="text-destructive text-sm">{{ errorMessage }}</p>
      <DialogFooter>
        <template v-if="isBlocked">
          <DialogClose as-child>
            <Button type="button" size="md" variant="outline">知道了</Button>
          </DialogClose>
          <Button as-child size="md">
            <RouterLink :to="{ name: 'settlement', params: { groupId } }">
              <ArrowRightLeft class="size-4.5" aria-hidden="true" />
              去結算
            </RouterLink>
          </Button>
        </template>
        <template v-else>
          <DialogClose as-child>
            <Button type="button" size="md" variant="outline" :disabled="isSubmitting">
              取消
            </Button>
          </DialogClose>
          <Button
            type="button"
            size="md"
            variant="destructive"
            :disabled="isSubmitting"
            @click="confirm"
          >
            <Spinner v-if="isSubmitting" />
            {{ member?.isMe ? '退出' : '移除' }}
          </Button>
        </template>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
