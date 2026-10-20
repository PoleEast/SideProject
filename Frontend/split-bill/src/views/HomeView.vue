<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { Archive, ChevronRight, Plus, ReceiptText, Zap } from '@lucide/vue'

import AccountMenu from '@/components/AccountMenu.vue'
import CloseGroupDialog from '@/components/CloseGroupDialog.vue'
import CreateGroupDrawer from '@/components/CreateGroupDrawer.vue'
import GroupListEmpty from '@/components/GroupListEmpty.vue'
import GroupListError from '@/components/GroupListError.vue'
import GroupListSkeleton from '@/components/GroupListSkeleton.vue'
import GroupRow from '@/components/GroupRow.vue'
import IconPattern from '@/components/IconPattern.vue'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useMyGroups } from '@/composables/useMyGroups'
import { sortForHome } from '@/lib/homeGroups'
import { useAuthStore } from '@/stores/auth'
import type { GroupListItemResponse, GroupResponse } from '@/types/group'

const authStore = useAuthStore()
const { groups, loadGroups, loadErrorMessage, loadState } = useMyGroups()

const isCloseDialogOpen = ref(false)
const closingGroup = ref<GroupListItemResponse | null>(null)
const isCreateDrawerOpen = ref(false)

const activeGroups = computed(() =>
  sortForHome(groups.value.filter((group) => group.closedAt === null)),
)

const closedCount = computed(() => groups.value.filter((group) => group.closedAt !== null).length)

const openCloseDialog = (group: GroupListItemResponse) => {
  closingGroup.value = group
  isCloseDialogOpen.value = true
}
const handleClosed = (updated: GroupResponse) => {
  if (closingGroup.value) {
    closingGroup.value.closedAt = updated.closedAt
  }
}
</script>

<template>
  <div class="mx-auto flex min-h-dvh max-w-md flex-col">
    <header class="flex h-16 shrink-0 items-center gap-2.5 pr-3 pl-4">
      <span
        class="bg-primary text-brand flex size-9 shrink-0 items-center justify-center rounded-sm"
      >
        <ReceiptText class="size-5" aria-hidden="true" />
      </span>
      <span class="flex-1 text-lg font-bold">Split Bill</span>
      <AccountMenu />
    </header>

    <CloseGroupDialog
      v-model:open="isCloseDialogOpen"
      :group="closingGroup"
      @closed="handleClosed"
    />

    <CreateGroupDrawer v-model:open="isCreateDrawerOpen" />

    <main class="flex flex-col gap-5 px-4 pt-1 pb-4">
      <section
        class="bg-brand dark:bg-card relative flex flex-col gap-4 overflow-hidden rounded-xl p-6"
      >
        <IconPattern :offset-x="40" :offset-y="-30" />
        <div class="relative flex flex-col gap-0.5">
          <p class="text-base font-semibold">{{ authStore.me ? `嗨，${authStore.me}` : '嗨！' }}</p>
          <h1 class="text-2xl font-bold tracking-tight">今天要跟誰分帳？</h1>
        </div>
        <div class="relative grid grid-cols-2 gap-2">
          <Button as-child size="md">
            <RouterLink :to="{ name: 'quick-expense' }">
              <Zap class="size-4.5" aria-hidden="true" />
              快速記一筆
            </RouterLink>
          </Button>
          <Button type="button" variant="secondary" size="md" @click="isCreateDrawerOpen = true">
            <Plus class="size-4.5" aria-hidden="true" />
            建立群組
          </Button>
        </div>
      </section>
      <section class="flex flex-col gap-2">
        <div class="flex items-baseline gap-1.5">
          <h2 class="text-lg font-bold">進行中</h2>
          <Skeleton v-if="loadState === 'loading'" class="bg-foreground/7 h-3 w-4 rounded-full" />
          <span
            v-else-if="loadState === 'ready'"
            class="text-muted-foreground text-base tabular-nums"
          >
            {{ activeGroups.length }}
          </span>
        </div>
        <GroupListSkeleton v-if="loadState === 'loading'" />
        <GroupListError
          v-else-if="loadState === 'error'"
          :message="loadErrorMessage"
          @retry="loadGroups"
        />
        <GroupListEmpty v-else-if="activeGroups.length === 0" />
        <div v-else class="bg-card shadow-card divide-y overflow-hidden rounded-xl">
          <GroupRow
            v-for="group in activeGroups"
            :key="group.id"
            :group="group"
            @close="openCloseDialog(group)"
          />
        </div>
      </section>
      <RouterLink
        v-if="loadState === 'ready' && closedCount > 0"
        :to="{ name: 'closed-groups' }"
        class="bg-card shadow-card flex h-14 items-center gap-3 rounded-xl px-4"
      >
        <Archive class="text-muted-foreground size-5" aria-hidden="true" />
        <span class="flex-1 text-base font-semibold">已結束</span>
        <span class="text-muted-foreground text-base tabular-nums">{{ closedCount }}</span>
        <ChevronRight class="size-4.5" aria-hidden="true" />
      </RouterLink>
    </main>
  </div>
</template>
