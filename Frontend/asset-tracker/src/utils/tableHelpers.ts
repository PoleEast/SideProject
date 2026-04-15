import { h } from 'vue'
import { NIcon, NTooltip } from 'naive-ui'
import { HelpOutlineRound } from '@vicons/material'

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
