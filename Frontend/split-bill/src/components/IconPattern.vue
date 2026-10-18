<script setup lang="ts">
import { useId } from 'vue'

import { Bed, Plane, ReceiptText, ShoppingBag, TramFront, Utensils } from '@lucide/vue'

const { offsetX = 0, offsetY = 0 } = defineProps<{
  offsetX?: number
  offsetY?: number
}>()

const patternId = useId()
</script>
<template>
  <svg class="text-foreground/10 absolute inset-0 size-full" aria-hidden="true">
    <defs>
      <pattern
        :id="patternId"
        :x="offsetX"
        :y="offsetY"
        width="720"
        height="416"
        patternUnits="userSpaceOnUse"
      >
        <g transform="translate(60 96) rotate(-14)">
          <ReceiptText :size="88" x="-44" y="-44" stroke-width="1.5" />
        </g>
        <g transform="translate(300 112) rotate(16)">
          <TramFront :size="96" x="-48" y="-48" stroke-width="1.5" />
        </g>
        <g transform="translate(540 100) rotate(-10)">
          <ShoppingBag :size="84" x="-42" y="-42" stroke-width="1.5" />
        </g>
        <g transform="translate(180 320) rotate(12)">
          <Utensils :size="84" x="-42" y="-42" stroke-width="1.5" />
        </g>
        <g transform="translate(420 304) rotate(10)">
          <Bed :size="92" x="-46" y="-46" stroke-width="1.5" />
        </g>
        <g transform="translate(660 316) rotate(-18)">
          <Plane :size="88" x="-44" y="-44" stroke-width="1.5" />
        </g>
      </pattern>
    </defs>
    <rect width="100%" height="100%" :fill="`url(#${patternId})`" />
  </svg>
</template>
