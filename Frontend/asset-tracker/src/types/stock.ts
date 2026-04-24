import type { CurrencyType, MarketType } from './common'

export interface BatchStockPriceRequest {
  stockMarket: MarketType
  code: string
}

export interface BatchStockPriceResponse {
  succeeded: stockPriceResponse[]
  failed: batchStockPriceFailure[]
}

export interface stockPriceResponse {
  stockMarket: MarketType
  code: string
  name: string
  closingPrice: number
  currency: CurrencyType
  date: string
}

export interface batchStockPriceFailure {
  stockMarket: MarketType
  code: string
  message: string
}
