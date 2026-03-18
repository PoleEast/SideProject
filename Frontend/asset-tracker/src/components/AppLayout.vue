<script setup lang="ts">
import {
  NLayout,
  NLayoutSider,
  NLayoutContent,
  NMenu,
  NButton,
  NText,
  NDivider,
  NIcon,
} from 'naive-ui'
import { useRouter, useRoute } from 'vue-router'
import { computed, h, type Component } from 'vue'
import type { MenuOption } from 'naive-ui'
import { BarChartRound, ListAltRound, ShowChartRound } from '@vicons/material'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()

function renderIcon(icon: Component) {
  return () => h(NIcon, null, { default: () => h(icon) })
}

const logout = () => {
  authStore.logout()
  router.push('/positions')
}

const activeKey = computed(() => route.path)

const menuOptions: MenuOption[] = [
  { label: '持倉總覽', key: '/positions', icon: renderIcon(BarChartRound) },
  { label: '交易記錄', key: '/transactions', icon: renderIcon(ListAltRound) },
  { label: '損益分析', key: '/pnl', icon: renderIcon(ShowChartRound) },
]

function handleMenuSelect(key: string) {
  router.push(key)
}
</script>

<template>
  <n-layout class="h-screen">
    <n-layout has-sider class="h-full">
      <!-- 側邊欄 -->
      <n-layout-sider
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
          <n-button block secondary @click="logout()">登出</n-button>
        </div>
      </n-layout-sider>

      <!-- 內容區 -->
      <n-layout-content content-style="padding: 24px; overflow-y: auto;">
        <slot />
      </n-layout-content>
    </n-layout>
  </n-layout>
</template>
