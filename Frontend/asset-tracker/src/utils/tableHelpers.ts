import { h } from 'vue'
import { NIcon, NTag, NText, NTooltip } from 'naive-ui'
import { HelpOutlineRound } from '@vicons/material'
import type { MarketType } from '@/types/common'
import { getPnlTextColor, marketColors } from './colors'

export const renderCurrencyHintTitle = (title: string) =>
  h('span', { style: 'display: flex; align-items: center; gap: 4px' }, [
    title,
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
  ])

export const renderMarketTag = (market: MarketType) => {
  const color = marketColors[market]
  const style = color
    ? `color: ${color.secondary}; background: ${color.primary}; font-weight: 600`
    : 'font-weight: 600'
  return h(NTag, { size: 'small', bordered: false, style }, { default: () => market })
}

export const renderPnlValue = (value?: number) => {
  const text = value?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '-'
  const color = getPnlTextColor(value)

  return h(NText, { strong: true, style: { color } }, { default: () => text })
}

export const renderPnlRate = (rate?: number) => {
  const text =
    rate !== undefined
      ? rate.toLocaleString(undefined, {
          style: 'percent',
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        })
      : '-'
  const color = getPnlTextColor(rate)

  return h(NText, { strong: true, style: { color } }, { default: () => text })
}

export const getPnlRowClassName = (pnl?: number) => {
  if (pnl === undefined) return ''

  return pnl > 0 ? 'row-profit' : pnl < 0 ? 'row-loss' : ''
}
