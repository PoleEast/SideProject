import type { CurrencyType, MarketType, TransactionType } from './common'

export interface TransactionRequest {
  stockCode: string
  market: MarketType
  date: string
  type: TransactionType
  price: number
  quantity: number
  currency: CurrencyType
  remark: string
}

export interface TransactionResponse extends TransactionRequest {
  id: number
  createdAt: Date
}
