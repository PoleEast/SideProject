<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <div
    data-slot="item-content"
    :class="cn('flex min-w-0 flex-1 flex-col [&+[data-slot=item-content]]:flex-none', props.class)"
  >
    <slot />
  </div>
</template>
