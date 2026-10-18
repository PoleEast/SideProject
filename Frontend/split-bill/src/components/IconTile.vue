<script setup lang="ts">
import type { Component } from 'vue'

import { tileColorClasses, type TileColor } from '@/lib/iconTile'

defineProps<{
  icon: Component
  color: TileColor
  label: string
}>()
</script>

<template>
  <span
    role="img"
    :aria-label="label"
    :class="[
      'flex size-10 shrink-0 items-center justify-center rounded-sm',
      tileColorClasses[color],
    ]"
  >
    <component :is="icon" class="size-5" aria-hidden="true" />
  </span>
</template>
