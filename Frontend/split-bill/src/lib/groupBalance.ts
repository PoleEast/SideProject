import { minorUnitDecimals } from '@/lib/money'
import type { CurrencyType } from '@/types/currency'
import type { MemberBalance } from '@/types/settlement'

export const getPersonalBalance = (
  balances: MemberBalance[],
  memberId: number,
  currency: CurrencyType,
) => {
  const scale = 10 ** minorUnitDecimals[currency]
  const myBalances = balances.filter(
    (balance) => balance.creditorMemberId === memberId || balance.debtorMemberId === memberId,
  )

  // 換成最小單位的整數再相加
  let netInMinorUnits = 0
  for (const balance of myBalances) {
    const amountInMinorUnits = Math.round(balance.amount * scale)
    netInMinorUnits +=
      balance.creditorMemberId === memberId ? amountInMinorUnits : -amountInMinorUnits
  }

  return { personalNet: netInMinorUnits / scale, isSettled: myBalances.length === 0 }
}
