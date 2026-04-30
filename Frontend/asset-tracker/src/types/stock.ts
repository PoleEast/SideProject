import type { CurrencyType, MarketType } from './common'

export interface StockIdentifier {
  stockMarket: MarketType
  code: string
}

export interface BatchStockPriceResponse {
  succeeded: StockPriceResponse[]
  failed: BatchStockPriceFailure[]
}

export interface StockPriceResponse {
  stockMarket: MarketType
  code: string
  name: string
  closingPrice: number
  currency: CurrencyType
  date: string
}

export interface BatchStockPriceFailure {
  stockMarket: MarketType
  code: string
  message: string
}

export interface BatchStockInfoResponse {
  succeeded: StockInfoResponse[]
  failed: BatchStockInfoFailure[]
}

export interface StockInfoResponse {
  stockMarket: MarketType
  code: string
  name: string
  industryCategory: string
  exchange?: string
}

export interface BatchStockInfoFailure {
  stockMarket: MarketType
  code: string
  message: string
}
