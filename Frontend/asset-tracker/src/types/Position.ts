import type { MarketType } from './common'

export interface PositionResponse {
  stockMarket: MarketType
  stockCode: string
  quantity: number
  averagePrice: number
}

export interface EnrichedPosition extends PositionResponse {
  convertedTotalCost: number
}
