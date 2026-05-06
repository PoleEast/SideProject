<script setup lang="ts">
import { NTag } from 'naive-ui'
import type { MarketType } from '@/types/common'
import { getMarketTagStyle } from '@/utils/marketHelpers'

defineProps<{ market: MarketType }>()
</script>

<template>
  <n-tag size="small" :bordered="false" :style="getMarketTagStyle(market)">
    {{ market }}
  </n-tag>
</template>
