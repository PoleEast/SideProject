<script setup lang="ts">
import { NSkeleton } from 'naive-ui'

const { rows = 5 } = defineProps<{ rows?: number }>()
</script>

<template>
  <div class="flex flex-col gap-3 p-2">
    <NSkeleton v-for="i in rows" :key="i" height="28px" :sharp="false" />
  </div>
</template>
