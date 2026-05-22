import { computed, ref, type Ref } from 'vue'

import type { EChartsOption } from 'echarts'
import type { CallbackDataParams } from 'echarts/types/dist/shared'
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
      formatter: (params) => {
        const p = (Array.isArray(params) ? params[0] : params) as CallbackDataParams
        const row = p.value as [string, string, number]
        const stockName = row?.[1] ?? ''
        const amount = Number(row?.[2] ?? 0).toLocaleString(undefined, {
          maximumFractionDigits: 0,
        })
        return `<div style="display:flex;align-items:center;gap:16px;font-weight:700">
                  <span>${p.marker}${p.name}</span>
                  <span style="margin-left:auto">${amount} (${p.percent}%)</span>
                </div>
                ${stockName ? `<div style="font-size:12px;color:#999;margin-left:14px">${stockName}</div>` : ''}`
      },
    },
    legend: {
      type: 'scroll',
      top: 'center',
      left: '80%',
      orient: 'vertical',
      itemWidth: 14,
      itemHeight: 14,
      itemGap: 8,
      formatter: (name: string) => {
        const total = stockDataSet.value.slice(1).reduce((sum, row) => sum + Number(row[2]), 0)
        const item = stockDataSet.value.find((row) => row[0] === name)
        const value = item ? Number(item[2]) : 0
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
        center: ['50%', '50%'],
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
