<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

import IconTile from '@/components/IconTile.vue'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { getAvatarColorClass, getInitial } from '@/lib/avatar'
import { expenseCategories } from '@/lib/expenseCategory'
import { describeMyShare } from '@/lib/expenseShare'
import { formatMoney } from '@/lib/money'
import type { ExpenseResponse } from '@/types/expense'
import type { GroupMemberResponse } from '@/types/groupMember'

const { groupId, expense, members, myMemberId } = defineProps<{
  groupId: number
  expense: ExpenseResponse
  members: GroupMemberResponse[]
  myMemberId: number
}>()

const category = computed(() => expenseCategories[expense.category])
const payer = computed(() => members.find((member) => member.id === expense.payerId))
const payerName = computed(() => (payer.value?.isMe ? '你' : (payer.value?.displayName ?? '')))
const payerInitial = computed(() => (payer.value?.isMe ? '你' : getInitial(payerName.value)))
const payerColorClass = computed(() => getAvatarColorClass(members, expense.payerId))
</script>

<template>
  <RouterLink
    :to="{ name: 'expense-edit', params: { groupId, expenseId: expense.id } }"
    class="flex items-center gap-3 px-4 py-3"
  >
    <span class="relative shrink-0">
      <IconTile :icon="category.icon" :color="category.color" :label="category.label" />
      <Avatar
        role="img"
        :aria-label="`付款人：${payerName}`"
        class="ring-card absolute -right-1 -bottom-1 size-5 ring-2"
      >
        <AvatarFallback :class="['text-xs leading-none font-bold text-stone-900', payerColorClass]">
          {{ payerInitial }}
        </AvatarFallback>
      </Avatar>
    </span>
    <span class="flex min-w-0 flex-1 flex-col">
      <span class="truncate text-base font-semibold">{{ expense.name }}</span>
      <span class="text-muted-foreground truncate text-sm tabular-nums">
        {{ describeMyShare(expense, myMemberId) }}
      </span>
    </span>
    <span class="shrink-0 text-base font-bold whitespace-nowrap tabular-nums">
      {{ formatMoney(expense.currency, expense.amount) }}
    </span>
  </RouterLink>
</template>
