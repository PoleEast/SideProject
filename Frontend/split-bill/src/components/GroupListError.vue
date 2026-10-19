<script setup lang="ts">
import { CloudOff, RefreshCw } from '@lucide/vue'

import { Button } from '@/components/ui/button'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'

defineProps<{
  message: string
}>()

const emit = defineEmits<{
  retry: []
}>()
</script>

<template>
  <Empty role="alert">
    <EmptyHeader>
      <EmptyMedia variant="icon" aria-hidden="true">
        <CloudOff />
      </EmptyMedia>
      <EmptyTitle>群組載入失敗</EmptyTitle>
      <EmptyDescription>{{ message }}</EmptyDescription>
    </EmptyHeader>
    <EmptyContent>
      <Button
        type="button"
        class="h-11 rounded-md px-5 text-base font-semibold"
        @click="emit('retry')"
      >
        <RefreshCw class="size-4.5" aria-hidden="true" />
        重試
      </Button>
    </EmptyContent>
  </Empty>
</template>
