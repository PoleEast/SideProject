import type { MarketType } from './common'

export interface PositionResponse {
  stockMarket: MarketType
  stockCode: string
  quantity: number
  averagePrice: number
}

export interface EnrichedPosition extends PositionResponse {
  stockName?: string
  totalCost: number
  currentPrice?: number
  unrealizedPnl?: number
  unrealizedPnlRate?: number
}

export interface DisplayedPosition extends EnrichedPosition {
  convertedTotalCost?: number
  convertedUnrealizedPnl?: number
}

export interface RealizedPnlResponse {
  id: number
  stockMarket: MarketType
  stockCode: string
  date: string
  sellQuantity: number
  sellPrice: number
  buyPrice: number
}

export interface EnrichedRealizedPnl extends RealizedPnlResponse {
  pnl: number
  convertedPnl: number | undefined
  pnlRate: number
}
