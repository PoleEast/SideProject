import type { MarketType, TransactionType } from './common'

export interface TransactionRequest {
  stockCode: string
  market: MarketType
  date: string
  type: TransactionType
  price: number
  quantity: number
  remark: string
}

export interface TransactionResponse extends TransactionRequest {
  id: number
  createdAt: Date
}
