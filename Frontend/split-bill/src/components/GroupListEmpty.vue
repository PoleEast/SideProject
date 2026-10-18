<script setup lang="ts">
import { ArrowUp } from '@lucide/vue'

import { groupIcons } from '@/lib/groupIcon'
import { tileColorClasses } from '@/lib/iconTile'

const tiles = [
  {
    item: groupIcons.Travel,
    size: 'size-12',
    radius: 'rounded-md',
    offset: '-mr-2 -rotate-10',
    icon: 'size-6',
  },
  {
    item: groupIcons.Dining,
    size: 'size-14',
    radius: 'rounded-lg',
    offset: 'relative z-10',
    icon: 'size-7',
  },
  {
    item: groupIcons.Home,
    size: 'size-12',
    radius: 'rounded-md',
    offset: '-ml-2 rotate-10',
    icon: 'size-6',
  },
]
</script>

<template>
  <div
    class="bg-card shadow-card flex flex-col items-center gap-4 rounded-xl px-6 py-8 text-center"
  >
    <div class="flex h-16 items-center justify-center" aria-hidden="true">
      <span
        v-for="tile in tiles"
        :key="tile.item.label"
        :class="['bg-card ring-card shrink-0 ring-3', tile.size, tile.radius, tile.offset]"
      >
        <span
          :class="[
            'flex size-full items-center justify-center',
            tile.radius,
            tileColorClasses[tile.item.color],
          ]"
        >
          <component :is="tile.item.icon" :class="tile.icon" />
        </span>
      </span>
    </div>
    <div class="flex flex-col gap-1">
      <p class="text-lg font-bold">還沒有進行中的群組</p>
      <p class="text-muted-foreground text-sm">
        出去玩、合租、聚餐都能開一個群組<br />誰該付多少，一眼就知道
      </p>
    </div>
    <p class="flex items-center gap-2 text-sm font-semibold">
      <span
        class="bg-brand text-primary flex size-6 items-center justify-center rounded-full"
        aria-hidden="true"
      >
        <ArrowUp class="size-3.5" />
      </span>
      從上面的「建立群組」開始
    </p>
  </div>
</template>
