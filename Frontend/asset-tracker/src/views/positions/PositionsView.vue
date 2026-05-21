<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { breakpointsTailwind, useBreakpoints } from '@vueuse/core'

import {
  NButton,
  NCard,
  NDataTable,
  NEmpty,
  NH2,
  NPopselect,
  NRadioButton,
  NRadioGroup,
  NSpin,
  NText,
} from 'naive-ui'

import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { PieChart } from 'echarts/charts'
import { CanvasRenderer } from 'echarts/renderers'
import {
  DatasetComponent,
  GraphicComponent,
  LegendComponent,
  TooltipComponent,
  TransformComponent,
} from 'echarts/components'

import { getPosition } from '@/api/Position'
import { getExchangeRate } from '@/api/exchangeRate'
import { getLatestStockPrices } from '@/api/stock'
import { currencies, marketCurrencyMap } from '@/constants/common'
import type { CurrencyType, MarketType } from '@/types/common'
import type { ExchangeRateResponse } from '@/types/exchangeRate'
import type { DisplayedPosition, EnrichedPosition, PositionResponse } from '@/types/Position'
import type { StockPriceResponse } from '@/types/stock'
import { getChartColor, getPnlTextColor, marketColors, pnlColors } from '@/utils/colors'
import MarketTag from '@/components/MarketTag.vue'
import ProportionBar, { type ProportionItem } from '@/components/ProportionBar.vue'
import TableSkeleton from '@/components/TableSkeleton.vue'
import CardListSkeleton from '@/components/CardListSkeleton.vue'

import { useMarketChart } from './useMarketChart'
import { usePositionColumns } from './usePositionColumns'
import { useStockChart } from './useStockChart'
import { useNetworkLoadingBar } from '@/composables/useNetworkLoadingBar'
import { useApiToast } from '@/composables/useApiToast'

use([
  CanvasRenderer,
  PieChart,
  TooltipComponent,
  LegendComponent,
  DatasetComponent,
  TransformComponent,
  GraphicComponent,
])

const router = useRouter()
const { handle } = useApiToast()
useNetworkLoadingBar()

const breakpoints = useBreakpoints(breakpointsTailwind)
const isMobile = breakpoints.smaller('md')

// ---- State ----

const positions = ref<PositionResponse[]>([])
const conversionRates = ref<ExchangeRateResponse>()
const stockPrices = ref<StockPriceResponse[]>([])

const isInitialLoading = ref<boolean>(true)
const isRefreshing = ref<boolean>(false)
const isInitError = ref<boolean>(false)

// 使用者選擇的顯示幣別
const displayCurrency = ref<CurrencyType>(currencies[0])
const currencyOptions = currencies.map((c) => ({ label: c, value: c }))

const enrichedPositions = computed<EnrichedPosition[]>(() =>
  positions.value.map<EnrichedPosition>((p) => {
    const stockPrice = stockPrices.value.find(
      (s) => s.stockMarket === p.stockMarket && s.code === p.stockCode,
    )

    return {
      ...p,
      stockName: stockPrice?.name,
      totalCost: p.averagePrice * p.quantity,
      currentPrice: stockPrice?.closingPrice,
      unrealizedPnl: stockPrice
        ? (stockPrice.closingPrice - p.averagePrice) * p.quantity
        : undefined,
      unrealizedPnlRate: stockPrice ? stockPrice.closingPrice / p.averagePrice - 1 : undefined,
    }
  }),
)

const displayedPositions = computed<DisplayedPosition[]>(() =>
  enrichedPositions.value.map<DisplayedPosition>((p) => {
    const conversionRate = conversionRates.value?.conversionRates[marketCurrencyMap[p.stockMarket]]

    return {
      ...p,
      convertedTotalCost: conversionRate ? p.totalCost / conversionRate : undefined,
      convertedUnrealizedPnl:
        p.unrealizedPnl !== undefined && conversionRate !== undefined
          ? p.unrealizedPnl / conversionRate
          : undefined,
    }
  }),
)

const marketDistribution = computed<ProportionItem[]>(() => {
  const totalMap = new Map<MarketType, number>()
  displayedPositions.value.forEach((p) => {
    totalMap.set(p.stockMarket, (totalMap.get(p.stockMarket) ?? 0) + (p.convertedTotalCost ?? 0))
  })

  const total = Array.from(totalMap.values()).reduce((sum, v) => sum + v, 0)

  return Array.from(totalMap.entries())
    .map(([market, value]) => ({
      key: market,
      label: market,
      percent: total > 0 ? value / total : 0,
      color: marketColors[market].chart,
    }))
    .sort((a, b) => b.percent - a.percent)
})

const stockDistribution = computed<ProportionItem[]>(() => {
  const total = displayedPositions.value.reduce((sum, v) => sum + (v.convertedTotalCost ?? 0), 0)

  return displayedPositions.value
    .map((d, index) => ({
      key: d.stockCode,
      label: d.stockCode,
      percent: total > 0 ? (d.convertedTotalCost ?? 0) / total : 0,
      color: getChartColor(index) ?? '',
    }))
    .sort((a, b) => b.percent - a.percent)
})

const { columns, rowProps, rowClassName } = usePositionColumns(displayCurrency, router)
const { stockChartRef, stockChartOption } = useStockChart(displayedPositions)
const {
  marketChartRef,
  marketChartOption,
  recalcMarketTotal,
  handleChartHover,
  handleChartClick,
  handleMarketLegendChange,
} = useMarketChart(displayedPositions, stockChartRef)

const formatPnl = (value: number | undefined) => {
  if (value === undefined) return '-'
  const prefix = value > 0 ? '+' : ''
  return prefix + value.toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}

// ---- Event Handlers ----

const handleCurrencyChange = async () => {
  isRefreshing.value = true
  const rateResult = await handle(getExchangeRate(displayCurrency.value))
  conversionRates.value = rateResult.ok ? rateResult.data : undefined
  recalcMarketTotal()

  isRefreshing.value = false
}

// ---- Lifecycle ----

const loadInitial = async () => {
  isInitialLoading.value = true
  isInitError.value = false

  const positionResult = await handle(getPosition())
  if (!positionResult.ok) {
    isInitError.value = true
    isInitialLoading.value = false
    return
  }
  positions.value = positionResult.data

  const rateResult = await handle(getExchangeRate(displayCurrency.value))
  if (rateResult.ok) conversionRates.value = rateResult.data

  const priceResult = await handle(
    getLatestStockPrices(
      positions.value.map((p) => ({
        stockMarket: p.stockMarket,
        code: p.stockCode,
      })),
      new Date(),
    ),
  )
  if (priceResult.ok) stockPrices.value = priceResult.data.succeeded

  recalcMarketTotal()
  isInitialLoading.value = false
}

onMounted(loadInitial)
</script>

<template>
  <!-- 頁首 -->
  <div v-if="!isMobile" class="mb-6 flex items-center justify-between">
    <n-h2 class="m-0!">
      <n-text type="primary">持倉總覽</n-text>
    </n-h2>
    <n-radio-group v-model:value="displayCurrency" @update:value="handleCurrencyChange">
      <n-radio-button v-for="currency in currencies" :key="currency" :value="currency">
        {{ currency }}
      </n-radio-button>
    </n-radio-group>
  </div>

  <!-- 初次載入：顯示 Skeleton -->
  <template v-if="isInitialLoading">
    <div v-if="!isMobile">
      <div class="mb-4 grid h-100 grid-cols-2 gap-4">
        <n-card size="small" bordered class="h-full">
          <div class="flex h-full items-center justify-center">
            <n-spin />
          </div>
        </n-card>
        <n-card size="small" bordered class="h-full">
          <div class="flex h-full items-center justify-center">
            <n-spin />
          </div>
        </n-card>
      </div>
      <n-card title="持倉明細" size="small" bordered>
        <TableSkeleton :rows="6" />
      </n-card>
    </div>
    <div v-else>
      <CardListSkeleton class="mb-3" :count="2" />
      <CardListSkeleton :count="4" />
    </div>
  </template>

  <template v-else>
    <!-- 載入失敗 -->
    <div v-if="isInitError" class="flex justify-center py-20">
      <n-empty size="large" description="無法載入持倉資料">
        <template #extra>
          <n-button type="primary" @click="loadInitial">重試</n-button>
        </template>
      </n-empty>
    </div>

    <template v-else-if="positions.length > 0">
      <div class="mb-4 grid gap-4" :class="isMobile ? 'grid-cols-1' : 'h-100 grid-cols-2'">
        <!-- 市場圓餅圖卡片 -->
        <n-card v-if="!isMobile" title="市場" size="small" bordered>
          <v-chart
            ref="marketChartRef"
            :option="marketChartOption"
            autoresize
            @mouseover="handleChartHover(true)"
            @globalout="handleChartHover(false)"
            @click="handleChartClick"
            @legendselectchanged="handleMarketLegendChange"
          ></v-chart>
        </n-card>

        <ProportionBar v-else title="市場分佈" :items="marketDistribution">
          <template #action>
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
          </template>
        </ProportionBar>

        <!-- 個股圓餅圖卡片 -->
        <n-card v-if="!isMobile" title="股票" class="h-100" size="small" bordered>
          <v-chart ref="stockChartRef" :option="stockChartOption" autoresize></v-chart>
        </n-card>

        <ProportionBar v-else title="個股分佈" :items="stockDistribution"></ProportionBar>
      </div>

      <!-- 持倉表格卡片 -->
      <n-card v-if="!isMobile" title="持倉明細" size="small" bordered>
        <n-data-table
          :columns="columns"
          :data="displayedPositions"
          :row-props="rowProps"
          :row-class-name="rowClassName"
          :loading="isRefreshing"
          :bordered="false"
          :scroll-x="700"
          striped
        />
      </n-card>

      <!-- 持倉卡片列表（手機版） -->
      <n-spin v-else :show="isRefreshing">
        <div class="flex flex-col gap-3">
          <n-card
            v-for="position in displayedPositions"
            :key="`${position.stockMarket}-${position.stockCode}`"
            size="medium"
            :bordered="false"
            content-style="padding: 0;"
            class="overflow-hidden shadow"
            @click="
              router.push({
                path: '/transactions',
                query: { stockCode: position.stockCode, stockMarket: position.stockMarket },
              })
            "
          >
            <div class="flex">
              <div
                class="w-1 shrink-0"
                :style="{
                  background:
                    position.unrealizedPnl !== undefined && position.unrealizedPnl > 0
                      ? pnlColors.profit.primary
                      : position.unrealizedPnl !== undefined && position.unrealizedPnl < 0
                        ? pnlColors.loss.primary
                        : 'transparent',
                }"
              />

              <div class="flex-1 p-3">
                <!-- 市場 + 代碼/名稱 + 現價 -->
                <div class="mb-2 flex items-center gap-2">
                  <MarketTag :market="position.stockMarket" />
                  <div class="flex min-w-0 flex-col leading-tight">
                    <n-text class="text-base font-semibold">{{ position.stockCode }}</n-text>
                    <n-text v-if="position.stockName" depth="3" class="truncate text-xs">
                      {{ position.stockName }}
                    </n-text>
                  </div>
                  <div
                    v-if="position.currentPrice !== undefined"
                    class="ml-auto flex shrink-0 items-baseline gap-2"
                  >
                    <n-text depth="3" class="text-xs">現價</n-text>
                    <n-text class="text-base font-semibold"> ${{ position.currentPrice }} </n-text>
                  </div>
                </div>

                <!-- 成本算式 + 未實現損益 -->
                <div class="flex items-end justify-between gap-2 text-sm">
                  <n-text class="min-w-0 self-end">
                    <span class="text-xs opacity-70">
                      {{ position.quantity }} 股 × ${{ position.averagePrice }} =
                    </span>
                    <span class="ml-1 font-semibold">
                      ${{
                        position.convertedTotalCost?.toLocaleString(undefined, {
                          maximumFractionDigits: 2,
                        }) ?? '-'
                      }}
                    </span>
                  </n-text>

                  <!-- 未實現損益 + 損益率 -->
                  <div class="flex shrink-0 flex-col items-end leading-tight whitespace-nowrap">
                    <n-text
                      :style="{ color: getPnlTextColor(position.unrealizedPnl) }"
                      class="text-lg font-bold"
                    >
                      {{ formatPnl(position.convertedUnrealizedPnl) }}
                    </n-text>
                    <n-text
                      v-if="position.unrealizedPnlRate !== undefined"
                      :style="{ color: getPnlTextColor(position.unrealizedPnl) }"
                      class="text-xs"
                    >
                      {{
                        position.unrealizedPnlRate.toLocaleString(undefined, {
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
      </n-spin>
    </template>

    <!-- 空資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="尚無持倉，前往交易記錄新增買入交易吧！" />
    </div>
  </template>
</template>
