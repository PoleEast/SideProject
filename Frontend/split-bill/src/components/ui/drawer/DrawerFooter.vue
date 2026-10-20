<script lang="ts" setup>
import type { HTMLAttributes } from "vue"
import { cn } from "cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <div
    data-slot="drawer-footer"
    :class="cn('mt-auto flex flex-col gap-2 px-5 pb-6', props.class)"
  >
    <slot />
  </div>
</template>
