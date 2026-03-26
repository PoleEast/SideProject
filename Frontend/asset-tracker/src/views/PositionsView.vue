<script setup lang="ts">
import {
  NDataTable,
  NTag,
  NH2,
  NText,
  NEmpty,
  NSpin,
  NAlert,
  NCard,
  NTooltip,
  NIcon,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import type { DataTableColumns } from 'naive-ui'
import type { MarketType } from '@/types/common'
import type { PositionResponse } from '@/types/Position'
import type { EChartsOption } from 'echarts'
import { marketColors } from '@/utils/colors'
import { HelpOutlineRound } from '@vicons/material'
import { getPosition } from '@/api/Position'
import VChart from 'vue-echarts'

import { use } from 'echarts/core'
import { PieChart } from 'echarts/charts'
import { CanvasRenderer } from 'echarts/renderers'
import {
  TooltipComponent,
  LegendComponent,
  DatasetComponent,
  TransformComponent,
  GraphicComponent,
} from 'echarts/components'

use([
  CanvasRenderer,
  PieChart,
  TooltipComponent,
  LegendComponent,
  DatasetComponent,
  TransformComponent,
  GraphicComponent,
])

// ---- State ----

const marketChartRef = ref<InstanceType<typeof VChart>>()
const stockChartRef = ref<InstanceType<typeof VChart>>()
const isHoveringChart = ref(false)
const errorMessage = ref('')
const isLoading = ref(false)
const positions = ref<PositionResponse[]>([])
// 追蹤市場圓餅圖目前被點擊選取的市場，用於 toggle 取消
const selectedMarket = ref<string | null>(null)
// 圓餅圖中央顯示的總金額，會隨 legend 篩選和扇區點擊動態更新
const marketTotal = ref(0)

// ---- Table ----

const columns: DataTableColumns<PositionResponse> = [
  {
    title: '市場',
    key: 'stockMarket',
    width: 80,
    render: (row) => {
      const color = marketColors[row.stockMarket]
      const style = color
        ? `color: ${color.secondary}; background: ${color.primary}; font-weight: 600`
        : 'font-weight: 600'
      return h(NTag, { size: 'small', bordered: false, style }, { default: () => row.stockMarket })
    },
  },
  { title: '股票代碼', key: 'stockCode', sorter: 'default' },
  {
    title: '持倉數量',
    key: 'quantity',
    sorter: 'default',
    render: (row) => row.quantity.toLocaleString(),
  },
  {
    title: '平均成本',
    key: 'averagePrice',
    sorter: 'default',
    render: (row) =>
      row.averagePrice.toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
  },
  {
    title: () =>
      h('span', { style: 'display: flex; align-items: center; gap: 4px' }, [
        '總成本',
        h(
          NTooltip,
          { trigger: 'hover' },
          {
            trigger: () =>
              h(
                NIcon,
                { size: 16, style: 'cursor: help; opacity: 0.5' },
                { default: () => h(HelpOutlineRound) },
              ),
            default: () => [
              '幣別由市場自動決定',
              h('br'),
              '台股 → 新台幣（TWD）',
              h('br'),
              '美股 → 美元（USD）',
              h('br'),
              '日股 → 日圓（JPY）',
            ],
          },
        ),
      ]),
    key: 'totalCost',
    sorter: (a, b) => a.averagePrice * a.quantity - b.averagePrice * b.quantity,
    render: (row) =>
      (row.averagePrice * row.quantity).toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
  },
]

// ---- Chart Dataset ----

// 將持倉資料按市場分組加總，作為市場圓餅圖的 dataset
const marketDataSet = computed(() => {
  const map = new Map<MarketType, number>()

  positions.value.forEach((pos) => {
    map.set(pos.stockMarket, (map.get(pos.stockMarket) ?? 0) + pos.averagePrice * pos.quantity)
  })

  return [['市場', '總額'], ...map.entries()]
})

// 個股圓餅圖的 dataset
const stockDataSet = computed(() => [
  ['股號', '總額'],
  ...positions.value.map((pos) => [pos.stockCode, pos.averagePrice * pos.quantity]),
])

// ---- Chart Options ----

const marketChartOption = computed<EChartsOption>(() => ({
  tooltip: {
    trigger: 'item',
  },
  legend: {
    orient: 'vertical',
    right: 10,
    top: 'center',
    selectedMode: 'multiple',
    formatter: (name) => {
      const item = marketDataSet.value.find((row) => row[0] === name)
      const value = item ? Number(item[1]).toLocaleString() : ''
      return `{name|${name}}\n{value|${value}}`
    },
    textStyle: {
      rich: {
        name: {
          fontWeight: 'bold',
        },
        value: {
          fontSize: 12,
          color: '#999',
        },
      },
    },
  },
  // 圓餅圖中央文字，hover 時淡出讓 emphasis label 顯示百分比
  graphic: [
    {
      type: 'text',
      left: '28%',
      top: 'center',
      invisible: isHoveringChart.value,
      style: {
        text: marketTotal.value.toLocaleString(),
        fontSize: 24,
        fontWeight: 'bold',
        textAlign: 'center',
        opacity: isHoveringChart.value ? 0 : 1,
      },
      transition: ['style'],
      transitionDuration: 500,
    },
  ],
  series: [
    {
      type: 'pie',
      center: ['40%', '50%'],
      radius: ['40%', '70%'],
      avoidLabelOverlap: false,
      itemStyle: {
        borderRadius: 10,
        borderColor: '#fff',
        borderWidth: 2,
      },
      label: {
        show: false,
        position: 'center',
      },
      emphasis: {
        label: {
          show: true,
          fontSize: 30,
          fontWeight: 'bold',
          formatter: isHoveringChart.value ? '{d}%' : '',
        },
      },
      selectedMode: 'single',
      selectedOffset: 0,
      select: {
        itemStyle: {
          borderWidth: 3,
          borderColor: '#222',
        },
      },
      labelLine: { show: false },
    },
  ],
  dataset: [
    {
      source: marketDataSet.value,
    },
    {
      transform: {
        type: 'sort',
        config: [
          {
            dimension: '總額',
            order: 'desc',
          },
        ],
      },
    },
  ],
}))

const stockChartOption = computed<EChartsOption>(() => ({
  tooltip: {
    trigger: 'item',
  },
  legend: {
    orient: 'vertical',
    right: 10,
    top: 'center',
    formatter: (name: string) => {
      const total = stockDataSet.value.slice(1).reduce((sum, row) => sum + Number(row[1]), 0)
      const item = stockDataSet.value.find((row) => row[0] === name)
      const value = item ? Number(item[1]) : 0
      const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0'
      return `{name|${name}} {percent|${percent}%}`
    },
    textStyle: {
      rich: {
        name: {
          fontWeight: 'bold',
        },
        percent: {
          fontSize: 12,
          color: '#999',
        },
      },
    },
  },
  series: [
    {
      type: 'pie',
      center: ['40%', '50%'],
      radius: ['30%', '70%'],
      itemStyle: {
        borderRadius: 10,
        borderColor: '#fff',
        borderWidth: 2,
      },
      label: {
        show: false,
      },
    },
  ],
  dataset: {
    source: stockDataSet.value,
  },
}))

// ---- Event Handlers ----

const handleChartHover = (hovering: boolean) => (isHoveringChart.value = hovering)

// Legend 點擊：同步篩選個股圖，並重置市場圖的選取狀態
const handleMarketLegendChange = (params: { name: string; selected: Record<string, boolean> }) => {
  selectedMarket.value = null

  const stockCodes = positions.value
    .filter((p) => !params.selected[p.stockMarket])
    .map((p) => p.stockCode)

  marketTotal.value = positions.value
    .filter((p) => params.selected[p.stockMarket])
    .reduce((sum, pos) => sum + pos.averagePrice * pos.quantity, 0)

  marketChartRef.value?.dispatchAction({
    type: 'unselect',
    seriesIndex: 0,
    dataIndex: marketDataSet.value.slice(1).map((_, i) => i),
  })

  stockChartRef.value?.dispatchAction({
    type: 'unselect',
    name: positions.value.map((p) => p.stockMarket),
  })

  stockChartRef.value?.dispatchAction({
    type: 'legendAllSelect',
  })

  stockCodes.forEach((code) => {
    stockChartRef.value?.dispatchAction({
      type: 'legendUnSelect',
      name: code,
    })
  })
}

// 扇區點擊：篩選個股圖只顯示該市場的股票，再點同一個還原全部
const handleChartClick = (params: { componentType: string; name: string; dataIndex: number }) => {
  if (params.componentType !== 'series') return

  if (selectedMarket.value === params.name) {
    selectedMarket.value = null
    marketTotal.value = positions.value.reduce(
      (sum, pos) => sum + pos.averagePrice * pos.quantity,
      0,
    )
    stockChartRef.value?.dispatchAction({ type: 'legendAllSelect' })
    return
  }

  selectedMarket.value = params.name

  const stockCodes = positions.value
    .filter((p) => params.name !== p.stockMarket)
    .map((p) => p.stockCode)

  marketTotal.value = positions.value
    .filter((p) => p.stockMarket === params.name)
    .reduce((sum, pos) => sum + pos.averagePrice * pos.quantity, 0)

  stockChartRef.value?.dispatchAction({
    type: 'legendAllSelect',
  })

  stockCodes.forEach((code) => {
    stockChartRef.value?.dispatchAction({
      type: 'legendUnSelect',
      name: code,
    })
  })
}

// ---- Lifecycle ----

onMounted(async () => {
  isLoading.value = true
  try {
    const result = await getPosition()

    if (!result.ok) {
      errorMessage.value = result.message
      return
    }

    positions.value = result.data
    marketTotal.value = positions.value.reduce(
      (sum, pos) => sum + pos.averagePrice * pos.quantity,
      0,
    )
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <!-- 頁首 -->
  <div class="mb-6">
    <n-h2 class="m-0!">
      <n-text type="primary">持倉總覽</n-text>
    </n-h2>
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
        <n-data-table :columns="columns" :data="positions" :bordered="false" striped />
      </n-card>
    </template>

    <!-- 空資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="尚無持倉，前往交易記錄新增買入交易吧！" />
    </div>
  </template>
</template>
