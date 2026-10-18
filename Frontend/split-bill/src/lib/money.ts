import type { CurrencyType } from '@/types/currency'

export const minorUnitDecimals: Record<CurrencyType, number> = {
  TWD: 0,
  JPY: 0,
  USD: 2,
}

export const formatMoney = (currency: CurrencyType, amount: number): string => {
  const decimals = minorUnitDecimals[currency]
  const formatted = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(amount)

  return `${currency} ${formatted}`
}
