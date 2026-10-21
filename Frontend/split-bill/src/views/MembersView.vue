<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { ChevronLeft, Plus } from '@lucide/vue'
import { toast } from 'vue-sonner'

import { getGroup } from '@/api/group'
import { getGroupMembers } from '@/api/groupMember'
import { getBalances } from '@/api/settlement'
import GroupLoadError from '@/components/group/GroupLoadError.vue'
import GroupNotFound from '@/components/group/GroupNotFound.vue'
import InviteLinkCard from '@/components/group/InviteLinkCard.vue'
import ResetInviteDialog from '@/components/group/ResetInviteDialog.vue'
import AddMemberDrawer from '@/components/member/AddMemberDrawer.vue'
import MemberRow from '@/components/member/MemberRow.vue'
import MembersPageSkeleton from '@/components/member/MembersPageSkeleton.vue'
import RemoveMemberDialog from '@/components/member/RemoveMemberDialog.vue'
import RenameMemberDrawer from '@/components/member/RenameMemberDrawer.vue'
import UnbindMemberDialog from '@/components/member/UnbindMemberDialog.vue'
import { Button } from '@/components/ui/button'
import { ItemGroup } from '@/components/ui/item'
import { getUnsettledCounterpartIds } from '@/lib/groupBalance'
import { listActiveMembers } from '@/lib/memberList'
import type { GroupResponse } from '@/types/group'
import type { GroupMemberResponse } from '@/types/groupMember'
import type { MemberBalance } from '@/types/settlement'

const route = useRoute()
const router = useRouter()
const groupId = Number(route.params.groupId)

const group = ref<GroupResponse | null>(null)
const members = ref<GroupMemberResponse[]>([])
const loadState = ref<'loading' | 'error' | 'notFound' | 'ready'>('loading')
const loadErrorMessage = ref('')
const isAddMemberOpen = ref(false)
const renamingMember = ref<GroupMemberResponse | null>(null)
const isRenameOpen = ref(false)
const balances = ref<MemberBalance[]>([])
const removingMember = ref<GroupMemberResponse | null>(null)
const isRemoveOpen = ref(false)
const unbindingMember = ref<GroupMemberResponse | null>(null)
const isUnbindOpen = ref(false)
const isResetOpen = ref(false)

const activeMembers = computed(() => listActiveMembers(members.value))

const retryLoad = async () => {
  loadState.value = 'loading'
  await loadPage()
}

const openRename = (member: GroupMemberResponse) => {
  renamingMember.value = member
  isRenameOpen.value = true
}

const openRemove = (member: GroupMemberResponse) => {
  removingMember.value = member
  isRemoveOpen.value = true
}

const openUnbind = (member: GroupMemberResponse) => {
  unbindingMember.value = member
  isUnbindOpen.value = true
}

// 名字要從含已移除成員的完整名單查，對方可能已經被移除了
const removingCounterpartNames = computed(() => {
  if (!removingMember.value) {
    return []
  }
  return getUnsettledCounterpartIds(balances.value, removingMember.value.id).map(
    (memberId) => members.value.find((member) => member.id === memberId)?.displayName ?? '',
  )
})

const handleLeft = () => {
  toast.success(`已退出 ${group.value?.name}`)
  router.push({ name: 'home' })
}

const handleInviteReset = async () => {
  toast.success('邀請連結已換新')
  await loadPage()
}

// 動作成功後也呼叫這裡重抓，所以不切回載入中，畫面才不會閃回骨架
const loadPage = async () => {
  const [groupResult, membersResult, balancesResult] = await Promise.all([
    getGroup(groupId),
    getGroupMembers(groupId),
    getBalances(groupId),
  ])

  if (!groupResult.ok || !membersResult.ok || !balancesResult.ok) {
    const failure = [groupResult, membersResult, balancesResult].find((result) => !result.ok)
    loadErrorMessage.value = failure?.message ?? ''
    loadState.value = failure?.status === 404 ? 'notFound' : 'error'
    return
  }

  group.value = groupResult.data
  members.value = membersResult.data
  balances.value = balancesResult.data
  loadState.value = 'ready'
}

onMounted(loadPage)
</script>

<template>
  <div class="mx-auto flex min-h-dvh max-w-md flex-col">
    <header class="flex h-16 shrink-0 items-center gap-0.5 px-1.5">
      <RouterLink
        v-if="loadState === 'error' || loadState === 'notFound'"
        :to="{ name: 'home' }"
        class="flex h-11 items-center gap-0.5 rounded-full pr-3.5 pl-2.5 text-base font-semibold"
      >
        <ChevronLeft class="size-6" aria-hidden="true" />
        回首頁
      </RouterLink>
      <template v-else>
        <Button as-child variant="ghost" size="icon" class="size-11 rounded-full">
          <RouterLink :to="{ name: 'group', params: { groupId } }" aria-label="回群組">
            <ChevronLeft class="size-6" aria-hidden="true" />
          </RouterLink>
        </Button>
        <h1 class="text-lg font-bold">成員</h1>
      </template>
    </header>

    <MembersPageSkeleton v-if="loadState === 'loading'" />
    <main v-else-if="loadState === 'error'" class="px-4 pt-1 pb-4">
      <GroupLoadError :message="loadErrorMessage" @retry="retryLoad" />
    </main>
    <main v-else-if="loadState === 'notFound'" class="px-4 pt-1 pb-4">
      <GroupNotFound />
    </main>
    <main
      v-else-if="loadState === 'ready' && group"
      class="flex flex-1 flex-col gap-5 px-4 pt-1 pb-4"
    >
      <InviteLinkCard
        :group-name="group.name"
        :invite-code="group.inviteCode"
        @reset="isResetOpen = true"
      />
      <section class="flex flex-col gap-2">
        <div class="flex items-center justify-between">
          <div class="flex items-baseline gap-1.5">
            <h2 class="text-lg font-bold">大家</h2>
            <span class="text-muted-foreground text-base tabular-nums">{{
              activeMembers.length
            }}</span>
          </div>
          <Button
            type="button"
            variant="ghost"
            class="h-11 gap-1 px-2 text-base font-semibold"
            @click="isAddMemberOpen = true"
          >
            <Plus class="size-4.5" aria-hidden="true" />
            新增成員
          </Button>
        </div>
        <ItemGroup class="bg-card shadow-card divide-y overflow-hidden rounded-xl">
          <MemberRow
            v-for="member in activeMembers"
            :key="member.id"
            :member="member"
            :members="members"
            @rename="openRename(member)"
            @remove="openRemove(member)"
            @unbind="openUnbind(member)"
          />
        </ItemGroup>
      </section>
      <AddMemberDrawer v-model:open="isAddMemberOpen" :group-id="groupId" @added="loadPage" />
      <RenameMemberDrawer
        v-model:open="isRenameOpen"
        :group-id="groupId"
        :member="renamingMember"
        @renamed="loadPage"
      />
      <RemoveMemberDialog
        v-model:open="isRemoveOpen"
        :group-id="groupId"
        :group-name="group.name"
        :member="removingMember"
        :counterpart-names="removingCounterpartNames"
        @removed="loadPage"
        @left="handleLeft"
      />
      <UnbindMemberDialog
        v-model:open="isUnbindOpen"
        :group-id="groupId"
        :member="unbindingMember"
        @unbound="loadPage"
      />
      <ResetInviteDialog
        v-model:open="isResetOpen"
        :group-id="groupId"
        @reset="handleInviteReset"
      />
    </main>
  </div>
</template>
