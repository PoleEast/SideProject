import { describe, expect, it } from 'vitest'

import { describeMyShare } from '@/lib/expenseShare'
import type { CurrencyType } from '@/types/currency'
import type { ExpenseResponse } from '@/types/expense'

const myMemberId = 1

const makeExpense = (
  payerId: number,
  amount: number,
  shares: Array<[memberId: number, amount: number]>,
  currency: CurrencyType = 'JPY',
): ExpenseResponse => ({
  id: 1,
  name: '晚餐',
  description: '',
  category: 'Food',
  currency,
  amount,
  rate: 1,
  date: '2026-10-20',
  payerId,
  shares: shares.map(([memberId, shareAmount]) => ({ memberId, amount: shareAmount })),
  createdAt: '2026-10-20T12:00:00+00:00',
})

describe('describeMyShare', () => {
  it('別人付的、你有分時，寫出你要付多少', () => {
    const expense = makeExpense(2, 12000, [
      [myMemberId, 3000],
      [2, 3000],
      [3, 3000],
      [4, 3000],
    ])

    expect(describeMyShare(expense, myMemberId)).toBe('你要付 JPY 3,000')
  })

  it('你付的時候，扣掉你自己的分攤就是你會收回的', () => {
    const expense = makeExpense(myMemberId, 3200, [
      [myMemberId, 800],
      [2, 800],
      [3, 800],
      [4, 800],
    ])

    expect(describeMyShare(expense, myMemberId)).toBe('你會收回 JPY 2,400')
  })

  it('你付的、但你沒分時，全額都會收回', () => {
    const expense = makeExpense(myMemberId, 1500, [[3, 1500]])

    expect(describeMyShare(expense, myMemberId)).toBe('你會收回 JPY 1,500')
  })

  it('別人付的、你也沒分', () => {
    const expense = makeExpense(2, 2000, [
      [2, 1000],
      [3, 1000],
    ])

    expect(describeMyShare(expense, myMemberId)).toBe('沒有你的事')
  })

  it('你付的、只分給你自己', () => {
    const expense = makeExpense(myMemberId, 1500, [[myMemberId, 1500]])

    expect(describeMyShare(expense, myMemberId)).toBe('只有你自己')
  })

  it('用原幣寫，不換算成基準幣', () => {
    const expense = makeExpense(
      2,
      30,
      [
        [myMemberId, 10],
        [2, 10],
        [3, 10],
      ],
      'USD',
    )

    expect(describeMyShare(expense, myMemberId)).toBe('你要付 USD 10.00')
  })

  it('美元相減的小數誤差不會出現在文字裡', () => {
    const expense = makeExpense(
      myMemberId,
      12.3,
      [
        [myMemberId, 4.1],
        [2, 8.2],
      ],
      'USD',
    )

    expect(describeMyShare(expense, myMemberId)).toBe('你會收回 USD 8.20')
  })
})
