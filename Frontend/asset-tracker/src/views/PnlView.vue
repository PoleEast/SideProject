<script lang="ts" setup>
import { computed, h, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import {
  NAlert,
  NCard,
  NDataTable,
  NEmpty,
  NFlex,
  NGi,
  NGrid,
  NH2,
  NIcon,
  NInput,
  NSelect,
  NSpin,
  NStatistic,
  NTag,
  NText,
  type DataTableColumns,
  type SelectOption,
} from 'naive-ui'
import { SearchRound } from '@vicons/material'

import { getRealizedPnl } from '@/api/Position'
import type { MarketType } from '@/types/common'
import type { RealizedPnlResponse } from '@/types/Position'
import { marketColors, pnlColors } from '@/utils/colors'

const router = useRouter()

// ---- State ----

const realizedPnl = ref<RealizedPnlResponse[]>([])
const isLoading = ref<boolean>(true)
const errorMessage = ref('')

// ---- Helper ----

const calcPnl = (record: RealizedPnlResponse) =>
  (record.sellPrice - record.buyPrice) * record.sellQuantity

const calcPnlRate = (record: RealizedPnlResponse) =>
  calcPnl(record) / (record.buyPrice * record.sellQuantity)

const formatPnl = (value: number) => {
  const prefix = value > 0 ? '+' : ''
  return (
    prefix +
    value.toLocaleString('zh-TW', {
      maximumFractionDigits: 2,
    })
  )
}

// ---- 篩選 ----

const filterMarket = ref<MarketType>()
const filterCode = ref('')

const marketOptions: SelectOption[] = [
  { label: '台股 TW', value: 'TW' },
  { label: '美股 US', value: 'US' },
  { label: '日股 JP', value: 'JP' },
]

const filteredRecords = computed(() => {
  const marketFilter = (record: RealizedPnlResponse) => {
    return !filterMarket.value || record.stockMarket === filterMarket.value
  }

  const codeFilter = (record: RealizedPnlResponse) => {
    return (
      !filterCode.value || record.stockCode.toUpperCase().includes(filterCode.value.toUpperCase())
    )
  }

  return realizedPnl.value.filter((record) => marketFilter(record) && codeFilter(record))
})

// ---- 統計 ----

const totalPnl = computed(() =>
  filteredRecords.value.reduce((sum, record) => sum + calcPnl(record), 0),
)
const winCount = computed(
  () => filteredRecords.value.filter((record) => record.sellPrice > record.buyPrice).length,
)
const lossCount = computed(
  () => filteredRecords.value.filter((record) => record.buyPrice > record.sellPrice).length,
)

// ---- Table ----

const columns: DataTableColumns<RealizedPnlResponse> = [
  { title: '日期', key: 'date', width: 110, render: (row) => row.date.slice(0, 10) },
  {
    title: '市場',
    key: 'stockMarket',
    width: 80,
    render: (row) => {
      const color = marketColors[row.stockMarket]
      const style = color
        ? `color: ${color.secondary}; background: ${color.primary}; font-weight: 600`
        : 'font-weight: 600'
      return h(
        NTag,
        { size: 'small', bordered: false, style: style },
        { default: () => row.stockMarket },
      )
    },
  },
  { title: '股票代碼', key: 'stockCode', width: 110 },
  { title: '買入價格', key: 'buyPrice', width: 100 },
  { title: '賣出價格', key: 'sellPrice', width: 100 },
  { title: '數量', key: 'sellQuantity', width: 80 },
  {
    title: '已實現損益',
    key: 'pnl',
    width: 120,
    render: (row) => {
      const pnl = calcPnl(row)
      const color =
        pnl > 0 ? pnlColors.profit.primary : pnl < 0 ? pnlColors.loss.primary : undefined
      return h(NText, { strong: true, style: { color } }, { default: () => formatPnl(pnl) })
    },
  },
  {
    title: '已實現損益(%)',
    key: 'pnlRate',
    width: 120,
    render: (row) => {
      const rate = calcPnlRate(row) * 100
      const color =
        rate > 0 ? pnlColors.profit.primary : rate < 0 ? pnlColors.loss.primary : undefined
      const text = `${rate > 0 ? '+' : ''}${rate.toFixed(2)} %`
      return h(NText, { strong: true, style: { color } }, { default: () => text })
    },
  },
]

const rowClassName = (row: RealizedPnlResponse) => {
  const pnl = calcPnl(row)
  return pnl > 0 ? 'row-profit' : pnl < 0 ? 'row-loss' : ''
}

const rowProps = (row: RealizedPnlResponse) => {
  const pnl = calcPnl(row)
  const rowRgb = pnl > 0 ? pnlColors.profit.rgb : pnl < 0 ? pnlColors.loss.rgb : ''
  return {
    style: {
      cursor: 'pointer',
      '--row-rgb': rowRgb,
    },
    onClick: () => {
      router.push({
        path: '/transactions',
        query: { stockCode: row.stockCode, stockMarket: row.stockMarket },
      })
    },
  }
}

// ---- Lifecycle ----

const loadRealizedPnl = async () => {
  isLoading.value = true

  try {
    const result = await getRealizedPnl()

    if (!result.ok) {
      errorMessage.value = result.message
      return
    }

    realizedPnl.value = result.data
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadRealizedPnl)
</script>

<template>
  <!-- 頁首 -->
  <div class="mb-6 flex items-center justify-between">
    <n-h2 class="m-0!">
      <n-text type="primary">損益分析</n-text>
    </n-h2>
  </div>

  <!-- 統計卡片 -->
  <n-grid cols="1 s:2 m:4" responsive="screen" :x-gap="16" :y-gap="16" class="mb-4">
    <n-gi>
      <n-card size="small" bordered class="h-full">
        <n-statistic label="獲利筆數">
          <n-text :style="{ color: pnlColors.profit.primary }" class="text-2xl font-bold">
            {{ winCount }}
          </n-text>
        </n-statistic>
      </n-card>
    </n-gi>
    <n-gi>
      <n-card size="small" bordered class="h-full">
        <n-statistic label="虧損筆數">
          <n-text :style="{ color: pnlColors.loss.primary }" class="text-2xl font-bold">
            {{ lossCount }}
          </n-text>
        </n-statistic>
      </n-card>
    </n-gi>
    <n-gi span="1 s:2 m:2">
      <n-card size="large" class="h-full shadow">
        <n-statistic label="已實現損益總計">
          <n-text
            :style="{
              color:
                totalPnl > 0
                  ? pnlColors.profit.primary
                  : totalPnl < 0
                    ? pnlColors.loss.primary
                    : undefined,
            }"
            class="text-4xl font-bold"
          >
            {{ formatPnl(totalPnl) }}
          </n-text>
        </n-statistic>
      </n-card>
    </n-gi>
  </n-grid>

  <!-- 篩選列 -->
  <n-flex class="mb-4" :wrap="false">
    <n-select
      v-model:value="filterMarket"
      :options="marketOptions"
      placeholder="市場"
      class="w-36"
      clearable
    />
    <n-input v-model:value="filterCode" placeholder="搜尋股票代碼" class="w-48" clearable>
      <template #prefix>
        <n-icon><SearchRound /></n-icon>
      </template>
    </n-input>
  </n-flex>

  <!-- 載入中 -->
  <div v-if="isLoading" class="flex justify-center py-20">
    <n-spin size="large" />
  </div>

  <template v-else>
    <!-- 錯誤提示 -->
    <n-alert v-if="errorMessage" type="error" :bordered="false" class="mb-4">
      {{ errorMessage }}
    </n-alert>

    <!-- 表格資料 -->
    <n-card v-else-if="filteredRecords.length > 0" title="損益明細" size="small" bordered>
      <n-data-table
        :columns="columns"
        :data="filteredRecords"
        :row-class-name="rowClassName"
        :row-props="rowProps"
        :bordered="false"
      />
    </n-card>

    <!-- 空資料 -->
    <div v-else-if="realizedPnl.length === 0" class="flex justify-center py-20">
      <n-empty description="尚無已實現損益，賣出持股後就會在這裡顯示！" />
    </div>

    <!-- 篩選後無資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="找不到符合條件的損益記錄" />
    </div>
  </template>
</template>

<style scoped>
:deep(.row-profit .n-data-table-td),
:deep(.row-loss .n-data-table-td) {
  background-color: rgba(var(--row-rgb), 0.08) !important;
  transition: background-color 0.2s ease;
}
:deep(.row-profit:hover .n-data-table-td),
:deep(.row-loss:hover .n-data-table-td) {
  background-color: rgba(var(--row-rgb), 0.13) !important;
}
</style>
