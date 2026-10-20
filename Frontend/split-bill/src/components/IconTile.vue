<script setup lang="ts">
import type { Component } from 'vue'

import { tileColorClasses, type TileColor } from '@/lib/iconTile'

const { size = 'md' } = defineProps<{
  icon: Component
  color: TileColor
  label: string
  size?: 'sm' | 'md' | 'lg'
}>()

const sizeClasses = {
  sm: { tile: 'size-7.5 rounded-sm', icon: 'size-4' },
  md: { tile: 'size-10 rounded-sm', icon: 'size-5' },
  lg: { tile: 'size-12 rounded-md', icon: 'size-6' },
}
</script>

<template>
  <span
    role="img"
    :aria-label="label"
    :class="[
      'flex shrink-0 items-center justify-center',
      sizeClasses[size].tile,
      tileColorClasses[color],
    ]"
  >
    <component :is="icon" :class="sizeClasses[size].icon" aria-hidden="true" />
  </span>
</template>
