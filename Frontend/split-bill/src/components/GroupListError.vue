<script setup lang="ts">
import { CloudOff, RefreshCw } from '@lucide/vue'

import { Button } from '@/components/ui/button'

defineProps<{
  message: string
}>()

const emit = defineEmits<{
  retry: []
}>()
</script>

<template>
  <div
    role="alert"
    class="bg-card shadow-card flex flex-col items-center gap-3 rounded-xl px-6 py-7 text-center"
  >
    <span
      class="bg-muted text-muted-foreground flex size-12 items-center justify-center rounded-md"
      aria-hidden="true"
    >
      <CloudOff class="size-6" />
    </span>
    <div class="flex flex-col gap-1">
      <p class="text-lg font-bold">群組載入失敗</p>
      <p class="text-muted-foreground text-sm">{{ message }}</p>
    </div>
    <Button
      type="button"
      class="h-11 rounded-md px-5 text-base font-semibold"
      @click="emit('retry')"
    >
      <RefreshCw class="size-4.5" aria-hidden="true" />
      重試
    </Button>
  </div>
</template>
