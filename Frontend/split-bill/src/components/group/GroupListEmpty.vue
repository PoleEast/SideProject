<script setup lang="ts">
import { ArrowUp } from '@lucide/vue'

import IconStack from '@/components/icon/IconStack.vue'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { groupIcons } from '@/lib/groupIcon'
</script>

<template>
  <Empty>
    <EmptyHeader>
      <EmptyMedia class="h-16" aria-hidden="true">
        <IconStack :left="groupIcons.Travel" :center="groupIcons.Dining" :right="groupIcons.Home" />
      </EmptyMedia>
      <EmptyTitle>還沒有進行中的群組</EmptyTitle>
      <EmptyDescription>
        出去玩、合租、聚餐都能開一個群組<br />誰該付多少，一眼就知道
      </EmptyDescription>
    </EmptyHeader>
    <EmptyContent>
      <p class="flex items-center gap-2 text-sm font-semibold">
        <span
          class="bg-brand text-stone-900 flex size-6 items-center justify-center rounded-full"
          aria-hidden="true"
        >
          <ArrowUp class="size-3.5" />
        </span>
        從上面的「建立群組」開始
      </p>
    </EmptyContent>
  </Empty>
</template>
