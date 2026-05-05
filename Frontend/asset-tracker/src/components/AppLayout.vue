<script setup lang="ts">
import { computed, h, type Component } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  NLayout,
  NLayoutSider,
  NLayoutHeader,
  NLayoutFooter,
  NLayoutContent,
  NMenu,
  NTabs,
  NTab,
  NButton,
  NText,
  NDivider,
  NIcon,
} from 'naive-ui'
import type { MenuOption } from 'naive-ui'
import { BarChartRound, ListAltRound, ShowChartRound, LogOutRound } from '@vicons/material'
import { useBreakpoints, breakpointsTailwind } from '@vueuse/core'
import { useAuthStore } from '@/stores/auth'

type NavItem = {
  label: string
  key: string
  icon: Component
}

const navItems: NavItem[] = [
  { label: '持倉總覽', key: '/positions', icon: BarChartRound },
  { label: '交易記錄', key: '/transactions', icon: ListAltRound },
  { label: '損益分析', key: '/pnl', icon: ShowChartRound },
]

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()
const breakpoints = useBreakpoints(breakpointsTailwind)
const isMobile = breakpoints.smaller('md')

const renderIcon = (icon: Component) => h(NIcon, null, { default: () => h(icon) })

const handleLogout = () => {
  authStore.logout()
  router.push('/positions')
}

const activeKey = computed(() => route.path)

const menuOptions = computed<MenuOption[]>(() =>
  navItems.map<MenuOption>((n) => ({
    label: n.label,
    key: n.key,
    icon: () => renderIcon(n.icon),
  })),
)

const handleMenuSelect = (key: string) => router.push(key)

const contentStyle = computed(() =>
  isMobile.value
    ? 'padding: 16px; padding-bottom: 80px; overflow-y: auto;'
    : 'padding: 24px; overflow-y: auto;',
)
</script>

<template>
  <n-layout class="h-screen" :has-sider="!isMobile">
    <!-- 桌面板側邊欄 -->
    <n-layout-sider
      v-if="!isMobile"
      bordered
      :width="240"
      :collapsed-width="0"
      content-style="display: flex; flex-direction: column; height: 100%;"
    >
      <!-- Logo -->
      <div class="flex items-center gap-2 px-4 py-4">
        <img src="/logoLight.svg" alt="Asset Tracker" class="h-8" />
        <n-text type="primary" class="text-xl font-bold">Asset Tracker</n-text>
      </div>

      <n-divider class="my-0!" />

      <!-- 頁面選單 -->
      <n-menu
        class="pt-4"
        :options="menuOptions"
        :value="activeKey"
        @update:value="handleMenuSelect"
      />

      <!-- 登出 -->
      <div class="mt-auto p-4">
        <n-divider class="mb-4!" />
        <n-button block secondary @click="handleLogout">登出</n-button>
      </div>
    </n-layout-sider>

    <!-- 手機板頂部 Header -->
    <n-layout-header
      v-if="isMobile"
      bordered
      class="flex shrink-0 items-center justify-between px-4 py-3"
    >
      <div class="flex items-center gap-2">
        <img src="/logoLight.svg" alt="Asset Tracker" class="h-7" />
        <n-text type="primary" class="text-lg font-bold">Asset Tracker</n-text>
      </div>
      <n-button quaternary circle @click="handleLogout" aria-label="登出">
        <template #icon>
          <n-icon :component="LogOutRound" />
        </template>
      </n-button>
    </n-layout-header>

    <!-- 共用內容區 -->
    <n-layout-content :content-style="contentStyle">
      <slot />
    </n-layout-content>

    <!-- 手機板底部 Tab Bar -->
    <n-layout-footer v-if="isMobile" position="absolute" bordered class="z-10 shrink-0">
      <n-tabs
        :value="activeKey"
        type="bar"
        justify-content="space-evenly"
        @update:value="handleMenuSelect"
      >
        <n-tab v-for="item in navItems" :key="item.key" :name="item.key">
          <div class="flex flex-col items-center gap-1 py-1">
            <n-icon :component="item.icon" :size="22" />
            <span class="text-xs">{{ item.label }}</span>
          </div>
        </n-tab>
      </n-tabs>
    </n-layout-footer>
  </n-layout>
</template>
