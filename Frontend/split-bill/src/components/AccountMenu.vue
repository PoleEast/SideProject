<script setup lang="ts">
import { computed } from 'vue'

import { LogOut } from '@lucide/vue'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { getInitial } from '@/lib/avatar'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const initial = computed(() => getInitial(authStore.me))
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <button
        type="button"
        aria-label="帳號選單"
        class="flex size-11 shrink-0 items-center justify-center rounded-full"
      >
        <Avatar class="size-9">
          <AvatarFallback class="bg-avatar-me text-base font-bold text-stone-900">
            {{ initial }}
          </AvatarFallback>
        </Avatar>
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" class="w-48">
      <DropdownMenuLabel class="truncate">{{ authStore.me }}</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuItem @select="authStore.logout()">
        登出
        <LogOut aria-hidden="true" />
      </DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
</template>
