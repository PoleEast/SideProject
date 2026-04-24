<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import {
  NDataTable,
  NH2,
  NText,
  NEmpty,
  NSpin,
  NAlert,
  NCard,
  NRadioGroup,
  NRadioButton,
} from 'naive-ui'

import VChart from 'vue-echarts'
import {
  TooltipComponent,
  LegendComponent,
  DatasetComponent,
  TransformComponent,
  GraphicComponent,
} from 'echarts/components'

import { use } from 'echarts/core'
import { PieChart } from 'echarts/charts'
import { CanvasRenderer } from 'echarts/renderers'

import { currencies, marketCurrencyMap } from '@/constants/common'
import type { CurrencyType } from '@/types/common'
import type { DisplayedPosition, EnrichedPosition, PositionResponse } from '@/types/Position'
import { getPosition } from '@/api/Position'
import { getExchangeRate } from '@/api/exchangeRate'
import { useMarketChart } from './useMarketChart'
import { usePositionColumns } from './usePositionColumns'
import { useStockChart } from './useStockChart'
import { getLatestStockPrices } from '@/api/stock'
import type { stockPriceResponse } from '@/types/stock'
import type { ExchangeRateResponse } from '@/types/exchangeRate'

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

// ---- State ----

const errorMessage = ref('')
const isLoading = ref(false)
const positions = ref<PositionResponse[]>([])
const conversionRates = ref<ExchangeRateResponse[]>([])
const stockPrices = ref<stockPriceResponse[]>([])

// 使用者選擇的顯示幣別
const displayCurrency = ref<CurrencyType>(currencies[0])

const { columns, rowProps } = usePositionColumns(displayCurrency, router)
const { stockChartRef, stockChartOption } = useStockChart(positions)
const {
  marketChartRef,
  marketChartOption,
  recalcMarketTotal,
  handleChartHover,
  handleChartClick,
  handleMarketLegendChange,
} = useMarketChart(positions, stockChartRef)

const enrichedPosition = computed<EnrichedPosition[]>(()=>
   positions.value.map<EnrichedPosition>(p => {
    const stockPrice = stockPrices.value.find(s=> s.stockMarket === p.stockMarket && s.code === p.stockCode)

    return{
    ...p,
    stockName: stockPrice?.name ?? '',
    totalCost: p.averagePrice * p.quantity,
    currentPrice: stockPrice?.closingPrice,
   unrealizedPnl: stockPrice ? (stockPrice.closingPrice - p.averagePrice) * p.quantity : undefined,
    unrealizedPnlRate: stockPrice?.closingPrice ? (stockPrice.closingPrice / p.averagePrice) - 1: undefined
  }}))

const displayedPositions = computed<DisplayedPosition[]>(()=>{

})

// ---- Helper ----
const calcConvertedTotalCost = (
  position: PositionResponse,
  conversionRates: Record<CurrencyType, number>,
) => {
  const currency = marketCurrencyMap[position.stockMarket]
  return (position.averagePrice * position.quantity) / conversionRates[currency]
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

    positions.value.forEach(
      (p) =>
        (p.convertedTotalCost = calcConvertedTotalCost(p, exchangeRateResult.data.conversionRates)),
    )

    recalcMarketTotal()
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
    const [positionResult, exchangeRateResult] = await Promise.all([
      getPosition(),
      getExchangeRate(displayCurrency.value),
    ])

    if (!positionResult.ok) {
      errorMessage.value = positionResult.message
      return
    }

    if (!exchangeRateResult.ok) {
      errorMessage.value = exchangeRateResult.message
      return
    }

    positions.value = positionResult.data.map((p) => ({
      ...p,
      convertedTotalCost: calcConvertedTotalCost(p, exchangeRateResult.data.conversionRates),
    }))
    recalcMarketTotal()

    const LatestStockPricesResult = await getLatestStockPrices(
      positions.value.map((p) => ({
        stockMarket: p.stockMarket,
        code: p.stockCode,
      })),
      new Date(),
    )

    if (!LatestStockPricesResult.ok) {
      errorMessage.value = LatestStockPricesResult.message
      return
    }

    const stockPrices = LatestStockPricesResult.data.succeeded

    positions.value.forEach((p) => {
      const stockPrice = stockPrices.find(
        (s) => s.code === p.stockCode && s.stockMarket === p.stockMarket,
      )

      p.stockName = stockPrice?.name
      p.currentPrice = stockPrice?.closingPrice
      p.unrealizedPnl =
    })
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <!-- 頁首 -->
  <div class="mb-6 flex items-center justify-between">
    <n-h2 class="m-0!">
      <n-text type="primary">持倉總覽</n-text>
    </n-h2>
    <n-radio-group v-model:value="displayCurrency" @update:value="handleCurrencyChange">
      <n-radio-button v-for="currency in currencies" :key="currency" :value="currency">
        {{ currency }}
      </n-radio-button>
    </n-radio-group>
  </div>

  <!-- 載入中 -->
  <div v-if="isLoading" class="flex justify-center py-20">
    <n-spin size="large" />
  </div>

  <template v-else>
    <!-- 錯誤提示 -->
    <n-alert v-if="errorMessage" type="error" :bordered="false" class="mb-4">
      {{ errorMessage }}
    </n-alert>

    <template v-else-if="positions.length > 0">
      <div class="mb-4 grid h-100 grid-cols-2 gap-4">
        <!-- 市場圓餅圖卡片 -->
        <n-card title="市場" size="small" bordered>
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
        <!-- 個股圓餅圖卡片 -->
        <n-card title="股票" size="small" bordered>
          <v-chart ref="stockChartRef" :option="stockChartOption" autoresize></v-chart>
        </n-card>
      </div>

      <!-- 持倉表格卡片 -->
      <n-card title="持倉明細" size="small" bordered>
        <n-data-table
          :columns="columns"
          :data="positions"
          :row-props="rowProps"
          :bordered="false"
          striped
        />
      </n-card>
    </template>

    <!-- 空資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="尚無持倉，前往交易記錄新增買入交易吧！" />
    </div>
  </template>
</template>
