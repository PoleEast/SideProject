<script lang="ts" setup>
import { NButton, NDropdown, NIcon } from 'naive-ui'
import type { DropdownMixedOption } from 'naive-ui/es/dropdown/src/interface'
import { LogInRound } from '@vicons/material'

const options: DropdownMixedOption[] = [
  {
    label: '設定',
    key: 'setting',
  },
  {
    label: '登出',
    key: 'logout',
  },
]
</script>

<template>
  <n-dropdown trigger="hover" :options="options">
    <n-button>
      登入
      <template #icon>
        <n-icon>
          <LogInRound />
        </n-icon>
      </template>
    </n-button>
  </n-dropdown>
</template>
