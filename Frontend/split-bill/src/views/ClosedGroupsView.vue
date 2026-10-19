<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

import { Archive, ArrowLeft, ChevronLeft } from '@lucide/vue'
import { toast } from 'vue-sonner'

import { setGroupClosed } from '@/api/group'
import GroupListError from '@/components/GroupListError.vue'
import GroupListSkeleton from '@/components/GroupListSkeleton.vue'
import GroupRow from '@/components/GroupRow.vue'
import { Button } from '@/components/ui/button'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { useMyGroups } from '@/composables/useMyGroups'
import type { GroupListItemResponse } from '@/types/group'

const { groups, loadErrorMessage, loadGroups, loadState } = useMyGroups()

const closedGroups = computed(() =>
  groups.value
    .filter((group) => group.closedAt !== null)
    .sort((first, second) => Date.parse(second.closedAt!) - Date.parse(first.closedAt!)),
)

const reopenGroup = async (group: GroupListItemResponse) => {
  const result = await setGroupClosed(group.id, { closed: false })
  if (!result.ok) {
    toast.error(`${group.name} 沒有重新開啟`, { description: result.message })
    return
  }

  group.closedAt = result.data.closedAt
}
</script>

<template>
  <div class="mx-auto flex min-h-dvh max-w-md flex-col">
    <header class="flex h-16 shrink-0 items-center gap-0.5 pr-4 pl-1.5">
      <Button as-child variant="ghost" size="icon" class="size-11 rounded-full">
        <RouterLink :to="{ name: 'home' }" aria-label="回首頁">
          <ChevronLeft class="size-6" aria-hidden="true" />
        </RouterLink>
      </Button>
      <div class="flex items-baseline gap-1.5">
        <h1 class="text-lg font-bold">已結束</h1>
        <Skeleton v-if="loadState === 'loading'" class="bg-foreground/7 h-3 w-4 rounded-full" />
        <span
          v-else-if="loadState === 'ready'"
          class="text-muted-foreground text-base tabular-nums"
        >
          {{ closedGroups.length }}
        </span>
      </div>
    </header>

    <main class="flex flex-col px-4 pt-1 pb-4">
      <GroupListSkeleton v-if="loadState === 'loading'" />
      <GroupListError
        v-else-if="loadState === 'error'"
        :message="loadErrorMessage"
        @retry="loadGroups"
      />
      <Empty v-else-if="closedGroups.length === 0">
        <EmptyHeader>
          <EmptyMedia variant="icon" aria-hidden="true">
            <Archive />
          </EmptyMedia>
          <EmptyTitle>沒有已結束的群組</EmptyTitle>
          <EmptyDescription>在首頁按下「結束」的群組，會收在這裡</EmptyDescription>
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
      <div v-else class="bg-card shadow-card divide-y overflow-hidden rounded-xl">
        <GroupRow
          v-for="group in closedGroups"
          :key="group.id"
          :group="group"
          @reopen="reopenGroup(group)"
        />
      </div>
    </main>
  </div>
</template>
