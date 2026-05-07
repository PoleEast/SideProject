<script lang="ts" setup>
import { computed, h, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { breakpointsTailwind, useBreakpoints } from '@vueuse/core'
import {
  NAlert,
  NButton,
  NCard,
  NDataTable,
  NDivider,
  NEmpty,
  NFlex,
  NGi,
  NGrid,
  NH2,
  NIcon,
  NInput,
  NPopselect,
  NRadioButton,
  NRadioGroup,
  NSelect,
  NSpin,
  NStatistic,
  NText,
  type DataTableColumns,
  type SelectOption,
} from 'naive-ui'
import { SearchRound } from '@vicons/material'

import { getExchangeRate } from '@/api/exchangeRate'
import { getRealizedPnl } from '@/api/Position'
import { getLatestStockInfos } from '@/api/stock'
import MarketTag from '@/components/MarketTag.vue'
import { currencies, marketCurrencyMap } from '@/constants/common'
import type { CurrencyType, MarketType } from '@/types/common'
import type { EnrichedRealizedPnl, RealizedPnlResponse } from '@/types/Position'
import type { StockInfoResponse } from '@/types/stock'
import { getPnlRowColor, getPnlTextColor, pnlColors } from '@/utils/colors'
import { getPnlRowClassName, renderCurrencyHintTitle, renderTwoLine } from '@/utils/tableHelpers'

const router = useRouter()

// ---- State ----

const realizedPnl = ref<RealizedPnlResponse[]>([])
const conversionRates = ref<Record<CurrencyType, number>>()
const stockInfos = ref<StockInfoResponse[]>([])
const isLoading = ref<boolean>(true)
const errorMessage = ref('')
const displayCurrency = ref<CurrencyType>('TWD')

const breakpoints = useBreakpoints(breakpointsTailwind)
const isMobile = breakpoints.smaller('md')

const enrichedRealizedPnl = computed<EnrichedRealizedPnl[]>(() =>
  realizedPnl.value.map((r) => {
    const pnl = (r.sellPrice - r.buyPrice) * r.sellQuantity
    const rate = conversionRates.value?.[marketCurrencyMap[r.stockMarket]]
    const infos = stockInfos.value?.find(
      (s) => s.code === r.stockCode && s.stockMarket === r.stockMarket,
    )
    return {
      ...r,
      stockName: infos?.name,
      pnl,
      pnlRate: pnl / (r.buyPrice * r.sellQuantity),
      convertedPnl: rate ? pnl / rate : undefined,
    }
  }),
)

// ---- Helper ----

const formatPnl = (value: number | undefined) => {
  if (value === undefined) return '-'

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

const currencyOptions = currencies.map((c) => ({ label: c, value: c }))

const filteredRecords = computed(() => {
  const marketFilter = (record: EnrichedRealizedPnl) => {
    return !filterMarket.value || record.stockMarket === filterMarket.value
  }

  const codeFilter = (record: EnrichedRealizedPnl) => {
    return (
      !filterCode.value || record.stockCode.toUpperCase().includes(filterCode.value.toUpperCase())
    )
  }

  return enrichedRealizedPnl.value.filter((record) => marketFilter(record) && codeFilter(record))
})

// ---- 統計 ----

const totalPnl = computed<number | undefined>(() => {
  if (filteredRecords.value.some((r) => r.convertedPnl === undefined)) return

  return filteredRecords.value.reduce((sum, record) => sum + record.convertedPnl!, 0)
})
const winCount = computed(
  () => filteredRecords.value.filter((record) => record.sellPrice > record.buyPrice).length,
)
const lossCount = computed(
  () => filteredRecords.value.filter((record) => record.buyPrice > record.sellPrice).length,
)

// ---- Table ----

const columns: DataTableColumns<EnrichedRealizedPnl> = [
  { title: '日期', key: 'date', render: (row) => row.date.slice(0, 10) },
  {
    title: '市場',
    key: 'stockMarket',
    fixed: 'left',
    render: (row) => h(MarketTag, { market: row.stockMarket }),
  },
  {
    title: '股票代碼',
    key: 'stockCode',
    fixed: 'left',
    render: (row) => renderTwoLine(row.stockCode, row.stockName),
  },
  {
    title: () => renderCurrencyHintTitle('買入價格'),
    key: 'buyPrice',
  },
  {
    title: () => renderCurrencyHintTitle('賣出價格'),
    key: 'sellPrice',
  },
  { title: '數量', key: 'sellQuantity' },
  {
    title: '已實現損益',
    key: 'convertedPnl',
    fixed: 'right',
    render: (row) => {
      const color = getPnlTextColor(row.convertedPnl)
      return renderTwoLine(
        row.convertedPnl?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '-',
        row.pnlRate !== undefined
          ? row.pnlRate.toLocaleString(undefined, {
              style: 'percent',
              maximumFractionDigits: 2,
            })
          : undefined,
        { primaryColor: color, secondaryColor: color },
      )
    },
  },
]

const rowProps = (row: EnrichedRealizedPnl) => {
  const rowRgb = getPnlRowColor(row.pnl) ?? ''
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

// ---- Event Handlers ----

const handleCurrencyChange = async () => {
  isLoading.value = true
  try {
    const exchangeRateResult = await getExchangeRate(displayCurrency.value)

    if (!exchangeRateResult.ok) {
      errorMessage.value = exchangeRateResult.message
      return
    }

    conversionRates.value = exchangeRateResult.data.conversionRates
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
}

// ---- Lifecycle ----

onMounted(async () => {
  isLoading.value = true

  try {
    const [realizedPnlResult, exchangeRateResult] = await Promise.all([
      getRealizedPnl(),
      getExchangeRate(displayCurrency.value),
    ])

    if (!realizedPnlResult.ok) {
      errorMessage.value = realizedPnlResult.message
      return
    }

    if (!exchangeRateResult.ok) {
      errorMessage.value = exchangeRateResult.message
      return
    }

    realizedPnl.value = realizedPnlResult.data
    conversionRates.value = exchangeRateResult.data.conversionRates

    const stockInfosResult = await getLatestStockInfos(
      realizedPnlResult.data.map((r) => ({
        stockMarket: r.stockMarket,
        code: r.stockCode,
      })),
    )

    if (!stockInfosResult.ok) {
      errorMessage.value = stockInfosResult.message
      return
    }

    stockInfos.value = stockInfosResult.data.succeeded
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <!-- 頁首 -->
  <div v-if="!isMobile" class="mb-6 flex items-center justify-between">
    <n-h2 class="m-0!">
      <n-text type="primary">損益分析</n-text>
    </n-h2>
    <n-radio-group v-model:value="displayCurrency" @update:value="handleCurrencyChange">
      <n-radio-button v-for="currency in currencies" :key="currency" :value="currency">
        {{ currency }}
      </n-radio-button>
    </n-radio-group>
  </div>

  <!-- 統計卡片 -->
  <n-grid
    v-if="!isMobile"
    cols="1 s:2 m:4"
    responsive="screen"
    :x-gap="16"
    :y-gap="16"
    class="mb-4"
  >
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
              color: getPnlTextColor(totalPnl),
            }"
            class="text-4xl font-bold"
          >
            {{ formatPnl(totalPnl) }}
            <n-text depth="3" class="ml-1 text-lg font-normal">{{ displayCurrency }}</n-text>
          </n-text>
        </n-statistic>
      </n-card>
    </n-gi>
  </n-grid>

  <!-- 統計卡片（手機版） -->
  <n-card v-else class="mb-3 shadow" :bordered="false">
    <div class="mb-1 flex items-center justify-between gap-2">
      <n-text depth="3" class="text-sm">已實現損益總計</n-text>
      <n-popselect
        v-model:value="displayCurrency"
        :options="currencyOptions"
        trigger="click"
        @update:value="handleCurrencyChange"
      >
        <n-button size="tiny" secondary round type="info">
          {{ displayCurrency }}
        </n-button>
      </n-popselect>
    </div>

    <n-text :style="{ color: getPnlTextColor(totalPnl) }" class="block text-3xl font-bold">
      {{ formatPnl(totalPnl) }}
    </n-text>

    <div class="mt-2 flex items-center gap-4 text-sm">
      <div class="flex items-center gap-1">
        <n-text depth="3">獲利</n-text>
        <n-text :style="{ color: pnlColors.profit.primary }" class="font-semibold">
          {{ winCount }}
        </n-text>
      </div>
      <div class="flex items-center gap-1">
        <n-text depth="3">虧損</n-text>
        <n-text :style="{ color: pnlColors.loss.primary }" class="font-semibold">
          {{ lossCount }}
        </n-text>
      </div>
    </div>
  </n-card>

  <!-- 篩選列 -->
  <n-flex v-if="!isMobile" class="mb-4" :wrap="false">
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

  <!-- 篩選列（手機版） -->
  <n-flex v-else :wrap="false" class="mb-1 gap-2">
    <n-select
      v-model:value="filterMarket"
      :options="marketOptions"
      placeholder="市場"
      class="flex-1"
      clearable
    />
    <n-input v-model:value="filterCode" placeholder="搜尋股票代碼" class="flex-1" clearable>
      <template #prefix>
        <n-icon><SearchRound /></n-icon>
      </template>
    </n-input>
  </n-flex>

  <!-- 篩選與內容分界（手機版） -->
  <n-divider v-if="isMobile" class="my-3!" />

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
    <n-card
      v-else-if="!isMobile && filteredRecords.length > 0"
      title="損益明細"
      size="small"
      bordered
    >
      <n-data-table
        :columns="columns"
        :data="filteredRecords"
        :row-class-name="(row) => getPnlRowClassName(row.pnl)"
        :row-props="rowProps"
        :scroll-x="700"
        :bordered="false"
      />
    </n-card>

    <!-- 卡片資料（手機版） -->
    <div v-else-if="isMobile && filteredRecords.length > 0" class="flex flex-col gap-3">
      <n-card
        v-for="record in filteredRecords"
        :key="`${record.date}-${record.stockMarket}-${record.stockCode}`"
        size="medium"
        :bordered="false"
        content-style="padding: 0;"
        class="overflow-hidden shadow"
        @click="
          router.push({
            path: '/transactions',
            query: { stockCode: record.stockCode, stockMarket: record.stockMarket },
          })
        "
      >
        <div class="flex">
          <div
            class="w-1 shrink-0"
            :style="{
              background:
                record.pnl > 0
                  ? pnlColors.profit.primary
                  : record.pnl < 0
                    ? pnlColors.loss.primary
                    : 'transparent',
            }"
          />

          <div class="flex-1 p-3">
            <div class="mb-2 flex items-center gap-2">
              <MarketTag :market="record.stockMarket" />
              <div class="flex flex-col leading-tight">
                <n-text class="text-base font-semibold">{{ record.stockCode }}</n-text>
                <n-text v-if="record.stockName" depth="3" class="text-xs">
                  {{ record.stockName }}
                </n-text>
              </div>
              <n-text depth="3" class="ml-auto text-xs">
                {{ record.date.slice(0, 10) }}
              </n-text>
            </div>

            <div class="flex items-end justify-between text-sm">
              <div class="flex flex-col gap-0.5">
                <n-text depth="3" class="text-xs">
                  買 ${{ record.buyPrice }} → 賣 ${{ record.sellPrice }}
                </n-text>
                <n-text depth="3" class="text-xs">數量 {{ record.sellQuantity }}</n-text>
              </div>
              <div class="flex flex-col items-end leading-tight">
                <n-text
                  :style="{ color: getPnlTextColor(record.convertedPnl) }"
                  class="text-lg font-bold"
                >
                  {{ formatPnl(record.convertedPnl) }}
                </n-text>
                <n-text
                  v-if="record.pnlRate !== undefined"
                  :style="{ color: getPnlTextColor(record.convertedPnl) }"
                  class="text-xs"
                >
                  {{
                    record.pnlRate.toLocaleString(undefined, {
                      style: 'percent',
                      maximumFractionDigits: 2,
                    })
                  }}
                </n-text>
              </div>
            </div>
          </div>
        </div>
      </n-card>
    </div>

    <!-- 空資料 -->
    <div v-else-if="enrichedRealizedPnl.length === 0" class="flex justify-center py-20">
      <n-empty description="尚無已實現損益，賣出持股後就會在這裡顯示！" />
    </div>

    <!-- 篩選後無資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="找不到符合條件的損益記錄" />
    </div>
  </template>
</template>
