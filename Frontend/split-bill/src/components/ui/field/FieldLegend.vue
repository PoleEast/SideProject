<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/lib/utils"

const props = defineProps<{
  class?: HTMLAttributes["class"]
  variant?: "legend" | "label"
}>()
</script>

<template>
  <legend
    data-slot="field-legend"
    :data-variant="variant"
    :class="cn(
      'mb-3 font-medium',
      'data-[variant=legend]:text-base',
      'data-[variant=label]:mb-1.5 data-[variant=label]:text-sm data-[variant=label]:leading-none data-[variant=label]:font-semibold',
      props.class,
    )"
  >
    <slot />
  </legend>
</template>
