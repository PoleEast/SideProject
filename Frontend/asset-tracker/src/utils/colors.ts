import type { MarketType, TransactionType } from '@/types/common'

export const marketColors: Record<
  MarketType,
  { primary: string; secondary: string; chart: string }
> = {
  TW: { primary: '#1a3a6b', secondary: '#ffffff', chart: '#1d4ed8' },
  US: { primary: '#1d4ed8', secondary: '#ffffff', chart: '#10b981' },
  JP: { primary: '#8b0000', secondary: '#ffffff', chart: '#ea580c' },
}

export const chartPalette = [
  '#1d4ed8', // 藍
  '#10b981', // 綠
  '#ea580c', // 橙
  '#a855f7', // 紫
  '#f59e0b', // 黃
  '#06b6d4', // 青
  '#ec4899', // 粉
  '#84cc16', // 萊姆
  '#6366f1', // 靛
  '#14b8a6', // 蒂芬妮
]

export const getChartColor = (index: number): string | undefined =>
  chartPalette[index % chartPalette.length]

export const transactionTypeColors: Record<TransactionType, { primary: string; rgb: string }> = {
  Buy: { primary: '#d03050', rgb: '208, 48, 80' },
  Sell: { primary: '#18a058', rgb: '24, 160, 88' },
}

export const pnlColors = {
  profit: transactionTypeColors.Buy,
  loss: transactionTypeColors.Sell,
}

export const getPnlTextColor = (value: number | undefined): string | undefined => {
  if (value === undefined) return undefined
  if (value > 0) return pnlColors.profit.primary
  if (value < 0) return pnlColors.loss.primary
  return undefined
}

export const getPnlRowColor = (value: number | undefined): string | undefined => {
  if (value === undefined) return undefined
  if (value > 0) return pnlColors.profit.rgb
  if (value < 0) return pnlColors.loss.rgb
  return undefined
}
