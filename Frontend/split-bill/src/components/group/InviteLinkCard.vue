<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import { Copy, LinkIcon, RotateCcw, Share } from '@lucide/vue'
import { toast } from 'vue-sonner'

import IconPattern from '@/components/icon/IconPattern.vue'
import { Button } from '@/components/ui/button'

const { groupName, inviteCode } = defineProps<{
  groupName: string
  inviteCode: string
}>()

const emit = defineEmits<{
  reset: []
}>()

const router = useRouter()

const joinRoute = computed(() => router.resolve({ name: 'join-group', params: { inviteCode } }))
const inviteUrl = computed(() => new URL(joinRoute.value.href, window.location.origin).href)
// 部分桌機瀏覽器沒有系統分享選單
const canShare = typeof navigator.share === 'function'

const shareInviteLink = async () => {
  try {
    await navigator.share({
      text: `一起用 Split Bill 記「${groupName}」的分帳`,
      url: inviteUrl.value,
    })
  } catch {
    // 使用者自己關掉分享選單也會丟出錯誤
  }
}

const copyInviteLink = async () => {
  try {
    await navigator.clipboard.writeText(inviteUrl.value)
  } catch {
    toast.error('邀請連結沒有複製成功')
    return
  }
  toast.success('已複製邀請連結')
}
</script>

<template>
  <section
    class="bg-brand dark:bg-card relative flex flex-col gap-3 overflow-hidden rounded-xl px-4 pt-4 pb-5"
  >
    <IconPattern :offset-x="40" :offset-y="-30" />
    <div class="relative flex items-center justify-between">
      <h2 class="text-lg font-bold">邀請連結</h2>
      <Button
        type="button"
        variant="ghost"
        class="hover:bg-foreground/10 -mr-1 h-9 gap-1.5 px-2 text-sm font-semibold"
        @click="emit('reset')"
      >
        <RotateCcw class="size-4" aria-hidden="true" />
        重置
      </Button>
    </div>
    <div class="bg-secondary relative flex h-12 items-center gap-2.5 rounded-md px-3.5">
      <LinkIcon class="text-muted-foreground size-4.5 shrink-0" aria-hidden="true" />
      <span class="min-w-0 truncate tabular-nums">…{{ joinRoute.path }}</span>
    </div>
    <div class="relative flex gap-2">
      <Button v-if="canShare" type="button" size="md" class="flex-1" @click="shareInviteLink">
        <Share class="size-4.5" aria-hidden="true" />
        分享
      </Button>
      <Button type="button" variant="secondary" size="md" class="flex-1" @click="copyInviteLink">
        <Copy class="size-4.5" aria-hidden="true" />
        複製
      </Button>
    </div>
    <p class="dark:text-muted-foreground relative text-sm font-semibold">
      朋友點開連結、登入之後，就能認領自己的名字
    </p>
  </section>
</template>
