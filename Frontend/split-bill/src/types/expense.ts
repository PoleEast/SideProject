import type { CurrencyType } from '@/types/currency'

export type ExpenseCategoryType =
  | 'Food'
  | 'Transport'
  | 'Accommodation'
  | 'Shopping'
  | 'Entertainment'
  | 'Other'

export interface ExpenseShareResponse {
  memberId: number
  amount: number
}

export interface ExpenseResponse {
  id: number
  name: string
  description: string
  category: ExpenseCategoryType
  currency: CurrencyType
  amount: number
  rate: number
  date: string
  payerId: number
  shares: ExpenseShareResponse[]
  createdAt: string
}
