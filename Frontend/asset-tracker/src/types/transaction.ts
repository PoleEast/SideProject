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

export type MarketType = 'TW' | 'US' | 'JP'
export type TransactionType = 'Buy' | 'Sell'
export type CurrencyType = 'TWD' | 'USD' | 'JPY'
