<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { ArrowLeft, SearchX } from '@lucide/vue'

import { Button } from '@/components/ui/button'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
</script>

<template>
  <Empty>
    <EmptyHeader>
      <EmptyMedia variant="icon" aria-hidden="true">
        <SearchX />
      </EmptyMedia>
      <EmptyTitle>找不到這個群組</EmptyTitle>
      <EmptyDescription>可能已經被刪除，或你不是這個群組的成員</EmptyDescription>
    </EmptyHeader>
    <EmptyContent>
      <Button as-child size="md">
        <RouterLink :to="{ name: 'home' }">
          <ArrowLeft class="size-4.5" aria-hidden="true" />
          回首頁
        </RouterLink>
      </Button>
    </EmptyContent>
  </Empty>
</template>
