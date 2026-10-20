<script lang="ts" setup>
import type { HTMLAttributes } from "vue"
import { cn } from "cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <div
    data-slot="drawer-header"
    :class="cn('flex flex-col gap-0.5 pt-2 pr-3 pl-5', props.class)"
  >
    <slot />
  </div>
</template>
