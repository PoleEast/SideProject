import type { CurrencyType } from './common'

export interface ExchangeRateResponse {
  currency: CurrencyType
  conversionRates: Record<CurrencyType, number>
  date: string
}
