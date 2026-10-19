<script setup lang="ts">
import { ArrowUp } from '@lucide/vue'

import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
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
  <Empty>
    <EmptyHeader>
      <EmptyMedia class="h-16" aria-hidden="true">
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
      </EmptyMedia>
      <EmptyTitle>還沒有進行中的群組</EmptyTitle>
      <EmptyDescription>
        出去玩、合租、聚餐都能開一個群組<br />誰該付多少，一眼就知道
      </EmptyDescription>
    </EmptyHeader>
    <EmptyContent>
      <p class="flex items-center gap-2 text-sm font-semibold">
        <span
          class="bg-brand text-primary flex size-6 items-center justify-center rounded-full"
          aria-hidden="true"
        >
          <ArrowUp class="size-3.5" />
        </span>
        從上面的「建立群組」開始
      </p>
    </EmptyContent>
  </Empty>
</template>
