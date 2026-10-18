<script setup lang="ts">
import { Skeleton } from '@/components/ui/skeleton'

const rows = [
  { name: 'w-28', amount: 'w-22' },
  { name: 'w-20', amount: 'w-18' },
  { name: 'w-34', amount: 'w-20' },
  { name: 'w-28', amount: 'w-22' },
]
</script>

<template>
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
        <Skeleton class="bg-foreground/7 h-3 w-16 rounded-full" />
      </div>
      <div class="flex shrink-0 flex-col items-end gap-2">
        <Skeleton class="bg-foreground/7 h-2.5 w-12 rounded-full" />
        <Skeleton :class="['bg-foreground/7 h-3.5 rounded-full', row.amount]" />
      </div>
    </div>
  </div>
</template>
