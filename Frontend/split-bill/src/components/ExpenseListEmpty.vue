<script setup lang="ts">
import { ArrowDown, ArrowUp, Plus, UserPlus } from '@lucide/vue'

import IconStack from '@/components/IconStack.vue'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { expenseCategories } from '@/lib/expenseCategory'
</script>

<template>
  <Empty class="pt-7 pb-6">
    <EmptyHeader>
      <EmptyMedia class="h-16" aria-hidden="true">
        <IconStack
          :left="expenseCategories.Transport"
          :center="expenseCategories.Food"
          :right="expenseCategories.Accommodation"
        />
      </EmptyMedia>
      <EmptyTitle>還沒有支出</EmptyTitle>
      <EmptyDescription>吃飯、搭車、住宿，誰先付錢都記下來</EmptyDescription>
    </EmptyHeader>
    <EmptyContent>
      <div class="bg-background flex w-full flex-col gap-2.5 rounded-md px-4 py-3 text-left">
        <p class="flex items-center gap-2.5 font-semibold">
          <span
            class="bg-brand flex size-6 shrink-0 items-center justify-center rounded-full text-stone-900"
            aria-hidden="true"
          >
            <UserPlus class="size-3.5" />
          </span>
          <span class="flex-1">按上面的「邀請朋友」加人</span>
          <ArrowUp class="text-muted-foreground size-4" aria-hidden="true" />
        </p>
        <p class="flex items-center gap-2.5 font-semibold">
          <span
            class="bg-brand flex size-6 shrink-0 items-center justify-center rounded-full text-stone-900"
            aria-hidden="true"
          >
            <Plus class="size-3.5" />
          </span>
          <span class="flex-1">按下面的「記一筆」記帳</span>
          <ArrowDown class="text-muted-foreground size-4" aria-hidden="true" />
        </p>
      </div>
    </EmptyContent>
  </Empty>
</template>
