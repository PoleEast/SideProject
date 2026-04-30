import { computed, type Ref } from 'vue'
import type { Router } from 'vue-router'

import { type DataTableColumns } from 'naive-ui'

import type { CurrencyType } from '@/types/common'
import type { DisplayedPosition } from '@/types/Position'
import { getPnlRowColor } from '@/utils/colors'
import {
  renderCurrencyHintTitle,
  renderMarketTag,
  renderPnlRate,
  renderPnlValue,
} from '@/utils/tableHelpers'

export const usePositionColumns = (displayCurrency: Ref<CurrencyType>, router: Router) => {
  const columns = computed<DataTableColumns<DisplayedPosition>>(() => [
    {
      title: '市場',
      key: 'stockMarket',
      render: (row) => renderMarketTag(row.stockMarket),
    },
    { title: '股票代碼', key: 'stockCode', sorter: 'default' },
    { title: '公司名稱', key: 'stockName' },
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
      render: (row) => row.averagePrice.toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
    {
      title: '現價',
      key: 'currentPrice',
      render: (row) =>
        row.currentPrice?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '-',
    },
    {
      title: () => renderCurrencyHintTitle('總成本'),
      key: 'totalCost',
      sorter: 'default',
      render: (row) => row.totalCost.toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
    {
      title: `總成本(${displayCurrency.value})`,
      key: 'convertedTotalCost',
      sorter: 'default',
      render: (row) =>
        row.convertedTotalCost?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '-',
    },
    {
      title: '未實現損益',
      key: 'unrealizedPnl',
      sorter: 'default',
      render: (row) => renderPnlValue(row.unrealizedPnl),
    },
    {
      title: '未實現損益(%)',
      key: 'unrealizedPnlRate',
      sorter: 'default',
      render: (row) => renderPnlRate(row.unrealizedPnlRate),
    },
  ])

  const rowProps = (row: DisplayedPosition) => {
    const rowRgb = getPnlRowColor(row.unrealizedPnl)

    return {
      style: {
        cursor: 'pointer',
        '--row-rgb': rowRgb ?? '',
      },
      onClick: () => {
        router.push({
          path: '/transactions',
          query: { stockCode: row.stockCode, stockMarket: row.stockMarket },
        })
      },
    }
  }

  const rowClassName = (row: DisplayedPosition) => {
    if (row.unrealizedPnl === undefined) return ''

    return row.unrealizedPnl > 0 ? 'row-profit' : row.unrealizedPnl < 0 ? 'row-loss' : ''
  }

  return {
    columns,
    rowProps,
    rowClassName,
  }
}
