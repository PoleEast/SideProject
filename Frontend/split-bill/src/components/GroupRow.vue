<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

import IconTile from '@/components/IconTile.vue'
import { Button } from '@/components/ui/button'
import { groupIcons } from '@/lib/groupIcon'
import { formatMoney } from '@/lib/money'
import type { GroupListItemResponse } from '@/types/group'

const { group } = defineProps<{
  group: GroupListItemResponse
}>()

const emit = defineEmits<{
  close: []
  reopen: []
}>()

const icon = computed(() => groupIcons[group.icon])
const balanceLabel = computed(() => {
  if (group.isSettled) {
    return null
  }
  if (group.personalNet > 0) {
    return '你會收回'
  } else if (group.personalNet < 0) {
    return '你要付'
  }
  return '收付相抵'
})
const balanceAmount = computed(() => formatMoney(group.baseCurrency, Math.abs(group.personalNet)))
const isClosed = computed(() => group.closedAt !== null)
const subtitle = computed(() => {
  const members = `${group.activeMemberCount} 位成員`
  // 已結束的群組右側讓給「重新開啟」，金額改寫在副標
  if (isClosed.value && group.personalNet !== 0) {
    return `${balanceLabel.value} ${balanceAmount.value}`
  }
  if (isClosed.value && group.isSettled && !group.isEveryoneSettled) {
    return `${members} · 還有人沒結清`
  }
  if (group.isSettled) {
    return `${members} · 已結清`
  }
  if (group.personalNet === 0) {
    return `${members} · 還有帳沒清`
  }
  return members
})
</script>

<template>
  <div class="flex items-center gap-3 px-4 py-3">
    <RouterLink
      class="flex min-w-0 flex-1 items-center gap-3"
      :to="{ name: 'group', params: { groupId: group.id } }"
    >
      <IconTile :icon="icon.icon" :color="icon.color" :label="icon.label" />
      <span class="flex min-w-0 flex-1 flex-col">
        <span class="truncate text-base font-semibold">{{ group.name }}</span>
        <span class="text-muted-foreground truncate text-sm">{{ subtitle }}</span>
      </span>
      <span v-if="balanceLabel && !isClosed" class="flex shrink-0 flex-col items-end">
        <span class="text-muted-foreground text-xs">{{ balanceLabel }}</span>
        <span class="text-base font-bold whitespace-nowrap tabular-nums">{{ balanceAmount }}</span>
      </span>
    </RouterLink>
    <Button
      v-if="!isClosed && group.isEveryoneSettled"
      type="button"
      variant="soft"
      class="h-10 rounded-sm px-4 text-base font-semibold"
      @click="emit('close')"
    >
      結束
    </Button>
    <Button
      v-else-if="isClosed"
      type="button"
      variant="soft"
      class="h-10 rounded-sm px-4 text-base font-semibold"
      @click="emit('reopen')"
    >
      重新開啟
    </Button>
  </div>
</template>
