import { computed, ref, type Ref } from 'vue'

import type { EChartsOption } from 'echarts'
import type VChart from 'vue-echarts'

import type { DisplayedPosition } from '@/types/Position'

export const useStockChart = (positions: Ref<DisplayedPosition[]>) => {
  const stockChartRef = ref<InstanceType<typeof VChart>>()

  const stockDataSet = computed(() => [
    ['股號', '名稱', '總額'],
    ...positions.value.map((pos) => [
      pos.stockCode,
      pos.stockName ?? '',
      pos.convertedTotalCost ?? 0,
    ]),
  ])

  const stockChartOption = computed<EChartsOption>(() => ({
    tooltip: {
      trigger: 'item',
      valueFormatter: (value) =>
        Number(value).toLocaleString(undefined, { maximumFractionDigits: 0 }),
    },
    legend: {
      orient: 'vertical',
      right: 10,
      top: 'center',
      formatter: (name: string) => {
        const total = stockDataSet.value.slice(1).reduce((sum, row) => sum + Number(row[2]), 0)
        const item = stockDataSet.value.find((row) => row[0] === name)
        const stockName = item ? String(item[1]) : ''
        const value = item ? Number(item[2]) : 0
        const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0'

        return `{name|${name}} {stockName|${stockName}} {percent|${percent}%}`
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
        overflow: 'truncate',
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

  return {
    stockChartRef,
    stockChartOption,
  }
}
