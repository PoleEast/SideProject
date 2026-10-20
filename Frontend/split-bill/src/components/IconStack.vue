<script setup lang="ts">
import { computed } from 'vue'

import { tileColorClasses, type TileIcon } from '@/lib/iconTile'

const { left, center, right } = defineProps<{
  left: TileIcon
  center: TileIcon
  right: TileIcon
}>()

const tiles = computed(() => [
  { item: left, size: 'size-12', radius: 'rounded-md', offset: '-mr-2 -rotate-10', icon: 'size-6' },
  { item: center, size: 'size-14', radius: 'rounded-lg', offset: 'relative z-10', icon: 'size-7' },
  { item: right, size: 'size-12', radius: 'rounded-md', offset: '-ml-2 rotate-10', icon: 'size-6' },
])
</script>

<template>
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
</template>
