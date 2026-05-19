<script setup lang="ts">
import { NCard, NSkeleton } from 'naive-ui'

const { count = 3 } = defineProps<{ count?: number }>()
</script>

<template>
  <div class="flex flex-col gap-3">
    <NCard v-for="i in count" :key="i" size="medium" :bordered="false" class="shadow">
      <div class="flex flex-col gap-2">
        <NSkeleton height="18px" width="40%" :sharp="false" />
        <NSkeleton height="22px" :sharp="false" />
      </div>
    </NCard>
  </div>
</template>
