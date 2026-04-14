import type { MarketType, TransactionType } from '@/types/common'

export const marketColors: Record<MarketType, { primary: string; secondary: string }> = {
  TW: { primary: '#1a3a6b', secondary: '#ffffff' },
  US: { primary: '#1d4ed8', secondary: '#ffffff' },
  JP: { primary: '#8b0000', secondary: '#ffffff' },
}

export const transactionTypeColors: Record<TransactionType, { primary: string; rgb: string }> = {
  Buy: { primary: '#d03050', rgb: '208, 48, 80' },
  Sell: { primary: '#18a058', rgb: '24, 160, 88' },
}

export const pnlColors = {
  profit: transactionTypeColors.Buy,
  loss: transactionTypeColors.Sell,
}
