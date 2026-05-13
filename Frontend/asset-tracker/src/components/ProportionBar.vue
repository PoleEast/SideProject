<script lang="ts" setup>
import { NCard, NText } from 'naive-ui'

export interface ProportionItem {
  key: string
  label: string
  percent: number
  color: string
}

const props = defineProps<{
  title: string
  items: ProportionItem[]
}>()
</script>

<template>
  <n-card size="small" :bordered="false" class="shadow-md">
    <div class="mb-3 flex items-center justify-between">
      <n-text class="text-base font-semibold">{{ props.title }}</n-text>
      <slot name="action"></slot>
    </div>

    <!-- 比例條 -->
    <div class="mb-3 flex h-3 w-full overflow-hidden rounded-full bg-gray-100">
      <div
        v-for="item in props.items"
        :key="item.key"
        :style="{ width: `${item.percent * 100}%`, background: item.color }"
        class="h-full"
      />
    </div>

    <!-- Legend -->
    <div class="flex flex-wrap gap-x-4 gap-y-1 text-sm">
      <div v-for="item in items" :key="item.key" class="flex items-center gap-1">
        <span class="inline-block h-2.5 w-2.5 rounded-full" :style="{ background: item.color }" />
        <n-text class="text-xs">{{ item.label }}</n-text>
        <n-text depth="3" class="text-xs"> {{ (item.percent * 100).toFixed(1) }}% </n-text>
      </div>
    </div>
  </n-card>
</template>
