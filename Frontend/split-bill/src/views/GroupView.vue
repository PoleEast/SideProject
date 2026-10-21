<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

import { ArrowRightLeft, ChevronLeft, ChevronRight, Plus, UserPlus, Users } from '@lucide/vue'

import { getExpenses } from '@/api/expense'
import { getGroup } from '@/api/group'
import { getGroupMembers } from '@/api/groupMember'
import { getBalances } from '@/api/settlement'
import ExpenseListEmpty from '@/components/expense/ExpenseListEmpty.vue'
import ExpenseRow from '@/components/expense/ExpenseRow.vue'
import GroupDetailSkeleton from '@/components/group/GroupDetailSkeleton.vue'
import GroupLoadError from '@/components/group/GroupLoadError.vue'
import GroupNotFound from '@/components/group/GroupNotFound.vue'
import IconPattern from '@/components/icon/IconPattern.vue'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { getPersonalBalance } from '@/lib/groupBalance'
import { formatMoney } from '@/lib/money'
import type { ExpenseResponse } from '@/types/expense'
import type { GroupResponse } from '@/types/group'
import type { GroupMemberResponse } from '@/types/groupMember'
import type { MemberBalance } from '@/types/settlement'

const route = useRoute()
const groupId = Number(route.params.groupId)

const group = ref<GroupResponse | null>(null)
const members = ref<GroupMemberResponse[]>([])
const expenses = ref<ExpenseResponse[]>([])
const balances = ref<MemberBalance[]>([])
const loadState = ref<'loading' | 'error' | 'notFound' | 'ready'>('loading')
const loadErrorMessage = ref('')

const myMember = computed(() => members.value.find((member) => member.isMe))
const personalBalance = computed(() => {
  if (!group.value || !myMember.value) {
    return null
  }
  return getPersonalBalance(balances.value, myMember.value.id, group.value.baseCurrency)
})

const loadGroup = async () => {
  loadState.value = 'loading'
  const [groupResult, membersResult, expensesResult, balancesResult] = await Promise.all([
    getGroup(groupId),
    getGroupMembers(groupId),
    getExpenses(groupId),
    getBalances(groupId),
  ])

  if (!groupResult.ok || !membersResult.ok || !expensesResult.ok || !balancesResult.ok) {
    const failure = [groupResult, membersResult, expensesResult, balancesResult].find(
      (result) => !result.ok,
    )
    loadErrorMessage.value = failure?.message ?? ''
    loadState.value = failure?.status === 404 ? 'notFound' : 'error'
    return
  }

  group.value = groupResult.data
  members.value = membersResult.data
  expenses.value = expensesResult.data
  balances.value = balancesResult.data
  loadState.value = 'ready'
}

onMounted(loadGroup)
</script>

<template>
  <div class="mx-auto flex min-h-dvh max-w-md flex-col">
    <header class="flex h-16 shrink-0 items-center gap-0.5 px-1.5">
      <RouterLink
        v-if="loadState === 'error' || loadState === 'notFound'"
        :to="{ name: 'home' }"
        class="flex h-11 items-center gap-0.5 rounded-full pr-3.5 pl-2.5 text-base font-semibold"
      >
        <ChevronLeft class="size-6" aria-hidden="true" />
        回首頁
      </RouterLink>
      <Button v-else as-child variant="ghost" size="icon" class="size-11 rounded-full">
        <RouterLink :to="{ name: 'home' }" aria-label="回首頁">
          <ChevronLeft class="size-6" aria-hidden="true" />
        </RouterLink>
      </Button>
      <Skeleton v-if="loadState === 'loading'" class="bg-foreground/7 h-3.5 w-24 rounded-full" />
      <h1 v-else-if="group" class="min-w-0 flex-1 truncate text-lg font-bold">{{ group.name }}</h1>
      <Button
        v-if="loadState === 'ready'"
        as-child
        variant="ghost"
        size="icon"
        class="size-11 rounded-full"
      >
        <RouterLink :to="{ name: 'members', params: { groupId } }" aria-label="成員">
          <Users class="size-5" aria-hidden="true" />
        </RouterLink>
      </Button>
    </header>

    <GroupDetailSkeleton v-if="loadState === 'loading'" :group-id="groupId" />
    <main v-else-if="loadState === 'error'" class="px-4 pt-1 pb-4">
      <GroupLoadError :message="loadErrorMessage" @retry="loadGroup" />
    </main>
    <main v-else-if="loadState === 'notFound'" class="px-4 pt-1 pb-4">
      <GroupNotFound />
    </main>
    <template v-else-if="group && myMember">
      <main class="flex flex-1 flex-col gap-5 px-4 pt-1 pb-4">
        <section
          v-if="personalBalance"
          class="bg-brand dark:bg-card relative flex flex-col gap-4 overflow-hidden rounded-xl p-6"
        >
          <IconPattern :offset-x="40" :offset-y="-30" />
          <div
            v-if="expenses.length === 0 && personalBalance.isSettled"
            class="relative flex flex-col gap-1"
          >
            <p class="dark:text-brand text-2xl font-bold tracking-tight">還沒開始記帳</p>
            <p class="dark:text-muted-foreground text-base font-semibold">
              記下第一筆，誰該付多少就會自動算好
            </p>
          </div>
          <div v-else-if="personalBalance.personalNet !== 0" class="relative flex flex-col gap-0.5">
            <p class="dark:text-muted-foreground text-base font-semibold">
              {{ personalBalance.personalNet > 0 ? '大家還欠你' : '你還欠大家' }}
            </p>
            <p
              class="text-display dark:text-brand font-bold tracking-tight whitespace-nowrap tabular-nums"
            >
              {{ formatMoney(group.baseCurrency, Math.abs(personalBalance.personalNet)) }}
            </p>
          </div>
          <div v-else class="relative flex flex-col gap-1">
            <p class="dark:text-brand text-2xl font-bold tracking-tight">
              {{ personalBalance.isSettled ? '你已經結清了' : '收付剛好相抵' }}
            </p>
            <p class="dark:text-muted-foreground text-base font-semibold">
              {{
                personalBalance.isSettled
                  ? '沒有要收的錢，也沒有要付的錢'
                  : '有人欠你、你也欠別人，去結算看看'
              }}
            </p>
          </div>
          <div class="relative grid grid-cols-2 gap-2">
            <Button as-child size="md">
              <RouterLink :to="{ name: 'settlement', params: { groupId } }">
                <ArrowRightLeft class="size-4.5" aria-hidden="true" />
                去結算
              </RouterLink>
            </Button>
            <Button as-child variant="secondary" size="md">
              <RouterLink :to="{ name: 'members', params: { groupId } }">
                <UserPlus class="size-4.5" aria-hidden="true" />
                邀請朋友
              </RouterLink>
            </Button>
          </div>
        </section>

        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold">最近的支出</h2>
            <RouterLink
              :to="{ name: 'activities', params: { groupId } }"
              class="flex h-11 items-center gap-0.5 text-base font-semibold"
            >
              看動態
              <ChevronRight class="size-4.5" aria-hidden="true" />
            </RouterLink>
          </div>
          <ExpenseListEmpty v-if="expenses.length === 0" />
          <div v-else class="bg-card shadow-card divide-y overflow-hidden rounded-xl">
            <ExpenseRow
              v-for="expense in expenses"
              :key="expense.id"
              :group-id="groupId"
              :expense="expense"
              :members="members"
              :my-member-id="myMember.id"
            />
          </div>
        </section>
      </main>

      <div class="bg-background sticky bottom-0 px-4 pt-3 pb-4">
        <Button as-child size="lg" class="w-full">
          <RouterLink :to="{ name: 'expense-new', params: { groupId } }">
            <Plus class="size-5" aria-hidden="true" />
            記一筆
          </RouterLink>
        </Button>
      </div>
    </template>
  </div>
</template>
