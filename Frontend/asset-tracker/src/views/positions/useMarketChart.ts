import { computed, ref, type Ref } from 'vue'

import type { EChartsOption } from 'echarts'
import type VChart from 'vue-echarts'

import type { DisplayedPosition } from '@/types/Position'
import type { MarketType } from '@/types/common'

export const useMarketChart = (
  positions: Ref<DisplayedPosition[]>,
  stockChartRef: Ref<InstanceType<typeof VChart> | undefined>,
) => {
  const marketChartRef = ref<InstanceType<typeof VChart>>()

  const marketTotal = ref(0)
  const isHoveringChart = ref(false)
  const selectedMarket = ref<string | null>(null)

  const marketDataSet = computed(() => {
    const map = new Map<MarketType, number>()

    positions.value.forEach((position) => {
      map.set(
        position.stockMarket,
        (map.get(position.stockMarket) ?? 0) + (position.convertedTotalCost ?? 0),
      )
    })

    return [['市場', '總額'], ...map.entries()]
  })

  const marketChartOption = computed<EChartsOption>(() => ({
    tooltip: {
      trigger: 'item',
      valueFormatter: (value) =>
        Number(value).toLocaleString(undefined, { maximumFractionDigits: 0 }),
    },
    legend: {
      orient: 'vertical',
      right: 5,
      top: 'center',
      selectedMode: 'multiple',
      formatter: (name) => {
        const item = marketDataSet.value.find((row) => row[0] === name)
        const value = item
          ? Number(item[1]).toLocaleString(undefined, { maximumFractionDigits: 0 })
          : ''
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
    // hover 時淡出中央總額文字，讓 emphasis label 顯示百分比
    graphic: (() => {
      const text = marketTotal.value.toLocaleString(undefined, { maximumFractionDigits: 0 })
      const fontSize = text.length > 12 ? 14 : text.length > 9 ? 18 : 24
      return [
        {
          type: 'text',
          left: 'center',
          top: 'center',
          invisible: isHoveringChart.value,
          style: {
            text,
            fontSize,
            fontWeight: 'bold',
            opacity: isHoveringChart.value ? 0 : 1,
          },
          silent: false,
          transition: ['style'],
          transitionDuration: 500,
        },
      ]
    })(),
    series: [
      {
        type: 'pie',
        center: ['50%', '50%'],
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

  const handleChartHover = (hovering: boolean) => (isHoveringChart.value = hovering)

  // 扇區點擊：篩選個股圖只顯示該市場股票，再點同一個還原全部
  const handleChartClick = (params: { componentType: string; name: string; dataIndex: number }) => {
    if (params.componentType !== 'series') return

    if (selectedMarket.value === params.name) {
      selectedMarket.value = null
      marketTotal.value = positions.value.reduce(
        (sum, position) => sum + (position.convertedTotalCost ?? 0),
        0,
      )
      stockChartRef.value?.dispatchAction({ type: 'legendAllSelect' })
      return
    }

    selectedMarket.value = params.name

    const stockCodes = positions.value
      .filter((position) => params.name !== position.stockMarket)
      .map((position) => position.stockCode)

    marketTotal.value = positions.value
      .filter((position) => position.stockMarket === params.name)
      .reduce((sum, position) => sum + (position.convertedTotalCost ?? 0), 0)

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

  // Legend 點擊：同步篩選個股圖，並重置市場圖的選取狀態
  const handleMarketLegendChange = (params: {
    name: string
    selected: Record<string, boolean>
  }) => {
    selectedMarket.value = null

    const stockCodes = positions.value
      .filter((position) => !params.selected[position.stockMarket])
      .map((position) => position.stockCode)

    marketTotal.value = positions.value
      .filter((position) => params.selected[position.stockMarket])
      .reduce((sum, position) => sum + (position.convertedTotalCost ?? 0), 0)

    marketChartRef.value?.dispatchAction({
      type: 'unselect',
      seriesIndex: 0,
      dataIndex: marketDataSet.value.slice(1).map((_, i) => i),
    })

    stockChartRef.value?.dispatchAction({
      type: 'unselect',
      name: positions.value.map((position) => position.stockMarket),
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
  const recalcMarketTotal = () => {
    marketTotal.value = positions.value.reduce(
      (sum, position) => sum + (position.convertedTotalCost ?? 0),
      0,
    )
  }

  return {
    marketChartRef,
    marketChartOption,
    recalcMarketTotal,
    handleChartHover,
    handleMarketLegendChange,
    handleChartClick,
  }
}
