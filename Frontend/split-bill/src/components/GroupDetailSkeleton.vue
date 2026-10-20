<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { ArrowRightLeft, ChevronRight, UserPlus } from '@lucide/vue'

import IconPattern from '@/components/IconPattern.vue'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'

const { groupId } = defineProps<{
  groupId: number
}>()

const rows = [
  { name: 'w-28', amount: 'w-20' },
  { name: 'w-20', amount: 'w-18' },
  { name: 'w-34', amount: 'w-22' },
  { name: 'w-24', amount: 'w-18' },
]
</script>

<template>
  <main class="flex flex-1 flex-col gap-5 px-4 pt-1 pb-4">
    <section
      class="bg-brand dark:bg-card relative flex flex-col gap-4 overflow-hidden rounded-xl p-6"
    >
      <IconPattern :offset-x="40" :offset-y="-30" />
      <div class="relative flex flex-col gap-3 py-1" aria-hidden="true">
        <Skeleton class="bg-foreground/12 h-3.5 w-22 rounded-full" />
        <Skeleton class="bg-foreground/12 h-9 w-50 rounded-sm" />
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
      <div
        role="status"
        aria-label="載入中"
        class="bg-card shadow-card divide-y overflow-hidden rounded-xl"
      >
        <div
          v-for="(row, index) in rows"
          :key="index"
          class="flex items-center gap-3 px-4 py-3"
          aria-hidden="true"
        >
          <Skeleton class="bg-foreground/7 size-10 shrink-0 rounded-sm" />
          <div class="flex min-w-0 flex-1 flex-col gap-2">
            <Skeleton :class="['bg-foreground/7 h-3.5 rounded-full', row.name]" />
            <Skeleton class="bg-foreground/7 h-3 w-22 rounded-full" />
          </div>
          <Skeleton :class="['bg-foreground/7 h-3.5 shrink-0 rounded-full', row.amount]" />
        </div>
      </div>
    </section>
  </main>
</template>
