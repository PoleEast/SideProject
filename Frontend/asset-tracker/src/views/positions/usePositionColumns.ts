import { computed, type Ref } from 'vue'
import type { Router } from 'vue-router'

import { type DataTableColumns } from 'naive-ui'

import type { CurrencyType } from '@/types/common'
import type { DisplayedPosition } from '@/types/Position'
import { getPnlRowColor, getPnlTextColor } from '@/utils/colors'
import { renderCurrencyHintTitle, renderMarketTag, renderTwoLine } from '@/utils/tableHelpers'
import { marketCurrencyMap } from '@/constants/common'

export const usePositionColumns = (displayCurrency: Ref<CurrencyType>, router: Router) => {
  const columns = computed<DataTableColumns<DisplayedPosition>>(() => [
    {
      title: '市場',
      key: 'stockMarket',
      render: (row) => renderMarketTag(row.stockMarket),
    },
    {
      title: '股票',
      key: 'stockCode',
      sorter: 'default',
      render: (row) => renderTwoLine(row.stockCode, row.stockName),
    },
    {
      title: '持倉數量',
      key: 'quantity',
      sorter: 'default',
      render: (row) => row.quantity.toLocaleString(),
    },
    {
      title: '現價/成本',
      key: 'currentPrice',
      sorter: 'default',
      render: (row) =>
        row.currentPrice !== undefined
          ? renderTwoLine(
              row.currentPrice.toLocaleString(undefined, { maximumFractionDigits: 2 }),
              row.averagePrice.toLocaleString(undefined, { maximumFractionDigits: 2 }),
            )
          : row.averagePrice.toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
    {
      title: () => renderCurrencyHintTitle(`總成本 (${displayCurrency.value})`),
      key: 'convertedTotalCost',
      sorter: 'default',
      render: (row) =>
        row.convertedTotalCost !== undefined
          ? renderTwoLine(
              row.convertedTotalCost?.toLocaleString(undefined, { maximumFractionDigits: 2 }),
              marketCurrencyMap[row.stockMarket] +
                ' ' +
                row.totalCost.toLocaleString(undefined, { maximumFractionDigits: 2 }),
            )
          : row.totalCost.toLocaleString(undefined, { maximumFractionDigits: 2 }),
    },
    {
      title: '未實現損益',
      key: 'unrealizedPnl',
      sorter: 'default',
      render: (row) => {
        const color = getPnlTextColor(row.unrealizedPnl)
        return renderTwoLine(
          row.unrealizedPnl?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '-',
          row.unrealizedPnlRate !== undefined
            ? row.unrealizedPnlRate.toLocaleString(undefined, {
                style: 'percent',
                maximumFractionDigits: 2,
              })
            : undefined,
          { primaryColor: color, secondaryColor: color },
        )
      },
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
