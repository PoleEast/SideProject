import { computed, h, type Ref } from 'vue'
import type { Router } from 'vue-router'

import { NIcon, NTag, NTooltip, type DataTableColumns } from 'naive-ui'
import { HelpOutlineRound } from '@vicons/material'

import type { CurrencyType } from '@/types/common'
import type { EnrichedPosition } from '@/types/Position'
import { marketColors } from '@/utils/colors'

export const usePositionColumns = (displayCurrency: Ref<CurrencyType>, router: Router) => {
  const columns = computed<DataTableColumns<EnrichedPosition>>(() => [
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
          { size: 'small', bordered: false, style },
          { default: () => row.stockMarket },
        )
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
      render: (row) => row.averagePrice.toLocaleString(),
    },
    {
      title: () =>
        h('span', { style: 'display: flex; align-items: center; gap: 4px' }, [
          '總成本(原)',
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
        (row.averagePrice * row.quantity).toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
    {
      title: `換算（${displayCurrency.value}）`,
      key: 'convertedTotalCost',
      sorter: (a, b) => a.convertedTotalCost - b.convertedTotalCost,
      render: (row) =>
        row.convertedTotalCost.toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
  ])

  const rowProps = (row: EnrichedPosition) => ({
    style: 'cursor: pointer',
    onClick: () => {
      router.push({
        path: '/transactions',
        query: { stockCode: row.stockCode, stockMarket: row.stockMarket },
      })
    },
  })

  return {
    columns,
    rowProps,
  }
}
