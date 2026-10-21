<script setup lang="ts">
import { computed } from 'vue'

import { Ellipsis, LogOut, Pencil, Trash2, Unlink } from '@lucide/vue'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Item,
  ItemActions,
  ItemContent,
  ItemDescription,
  ItemMedia,
  ItemTitle,
} from '@/components/ui/item'
import { getAvatarColorClass, getInitial } from '@/lib/avatar'
import type { GroupMemberResponse } from '@/types/groupMember'

const { member, members } = defineProps<{
  member: GroupMemberResponse
  // 頭貼配色依完整名單的先後輪流，要傳含已移除成員的名單
  members: GroupMemberResponse[]
}>()

const emit = defineEmits<{
  rename: []
  remove: []
  unbind: []
}>()

const avatarColorClass = computed(() => getAvatarColorClass(members, member.id))
</script>

<template>
  <Item role="listitem" class="py-2.5 pr-1.5">
    <ItemMedia>
      <Avatar class="size-10">
        <AvatarFallback :class="['text-base font-bold text-stone-900', avatarColorClass]">
          {{ getInitial(member.displayName) }}
        </AvatarFallback>
      </Avatar>
    </ItemMedia>
    <ItemContent>
      <ItemTitle>
        <span class="min-w-0 truncate">{{ member.displayName }}</span>
        <Badge v-if="member.isMe" variant="soft">你</Badge>
        <Badge v-if="member.isOwner" variant="muted">擁有者</Badge>
      </ItemTitle>
      <ItemDescription>{{ member.isBound ? '已綁定' : '未綁定' }}</ItemDescription>
    </ItemContent>
    <ItemActions>
      <DropdownMenu>
        <DropdownMenuTrigger as-child>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            class="text-muted-foreground size-11 rounded-full"
            :aria-label="`${member.displayName}的選項`"
          >
            <Ellipsis class="size-5" aria-hidden="true" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" class="w-48">
          <DropdownMenuItem @select="emit('rename')">
            <Pencil class="size-4.5" aria-hidden="true" />
            改名
          </DropdownMenuItem>
          <DropdownMenuItem
            v-if="member.isBound && !member.isOwner && !member.isMe"
            @select="emit('unbind')"
          >
            <Unlink class="size-4.5" aria-hidden="true" />
            解除綁定
          </DropdownMenuItem>
          <template v-if="!member.isOwner">
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" @select="emit('remove')">
              <LogOut v-if="member.isMe" class="size-4.5" aria-hidden="true" />
              <Trash2 v-else class="size-4.5" aria-hidden="true" />
              {{ member.isMe ? '退出群組' : '移除成員' }}
            </DropdownMenuItem>
          </template>
        </DropdownMenuContent>
      </DropdownMenu>
    </ItemActions>
  </Item>
</template>
