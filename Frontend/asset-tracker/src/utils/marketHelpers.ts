import type { MarketType } from '@/types/common'
import { marketColors } from './colors'

export const getMarketTagStyle = (market: MarketType): string => {
  const color = marketColors[market]
  return color
    ? `color: ${color.secondary}; background: ${color.primary}; font-weight: 600`
    : 'font-weight: 600'
}
