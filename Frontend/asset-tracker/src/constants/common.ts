import type { CurrencyType, MarketType } from '@/types/common'

export const currencies = ['TWD', 'USD', 'JPY'] as const

export const marketCurrencyMap: Record<MarketType, CurrencyType> = {
  TW: 'TWD',
  US: 'USD',
  JP: 'JPY',
}
