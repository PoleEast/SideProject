<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <div
    data-slot="empty"
    :class="cn(
      'bg-card shadow-card flex min-w-0 flex-col items-center justify-center gap-4 text-balance rounded-xl px-6 py-8 text-center',
      props.class,
    )"
  >
    <slot />
  </div>
</template>
