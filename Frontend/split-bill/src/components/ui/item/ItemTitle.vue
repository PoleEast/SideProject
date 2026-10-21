<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <div
    data-slot="item-title"
    :class="cn('flex w-fit max-w-full items-center gap-1.5 text-base font-semibold', props.class)"
  >
    <slot />
  </div>
</template>
