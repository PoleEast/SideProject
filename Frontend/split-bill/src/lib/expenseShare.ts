import { formatMoney } from '@/lib/money'
import type { ExpenseResponse } from '@/types/expense'

export const describeMyShare = (expense: ExpenseResponse, memberId: number): string => {
  const myShare = expense.shares.find((share) => share.memberId === memberId)?.amount ?? 0

  if (expense.payerId !== memberId) {
    return myShare > 0 ? `你要付 ${formatMoney(expense.currency, myShare)}` : '沒有你的事'
  }

  const amountToReceive = expense.amount - myShare
  if (amountToReceive === 0) {
    return '只有你自己'
  }
  return `你會收回 ${formatMoney(expense.currency, amountToReceive)}`
}
