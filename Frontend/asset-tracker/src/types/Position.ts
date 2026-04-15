import type { MarketType } from './common'

export interface PositionResponse {
  stockMarket: MarketType
  stockCode: string
  quantity: number
  averagePrice: number
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

export interface EnrichedPosition extends PositionResponse {
  convertedTotalCost: number
}

export interface EnrichedRealizedPnl extends RealizedPnlResponse {
  pnl: number
  convertedPnl: number | undefined
  pnlRate: number
}
