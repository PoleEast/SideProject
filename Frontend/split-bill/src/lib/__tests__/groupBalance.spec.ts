import { describe, expect, it } from 'vitest'

import { getPersonalBalance, getUnsettledCounterpartIds } from '@/lib/groupBalance'
import type { MemberBalance } from '@/types/settlement'

const myMemberId = 1

const makeBalance = (
  debtorMemberId: number,
  creditorMemberId: number,
  amount: number,
): MemberBalance => ({ debtorMemberId, creditorMemberId, amount })

describe('getPersonalBalance', () => {
  it('別人欠你的加上去，你欠別人的扣掉', () => {
    const balances = [
      makeBalance(2, myMemberId, 20000),
      makeBalance(3, myMemberId, 15000),
      makeBalance(myMemberId, 4, 1600),
    ]

    expect(getPersonalBalance(balances, myMemberId, 'JPY')).toEqual({
      personalNet: 33400,
      isSettled: false,
    })
  })

  it('只有你欠別人時個人淨額是負數', () => {
    const balances = [makeBalance(myMemberId, 4, 1250)]

    expect(getPersonalBalance(balances, myMemberId, 'JPY')).toEqual({
      personalNet: -1250,
      isSettled: false,
    })
  })

  it('不計入其他成員之間的兩兩淨額', () => {
    const balances = [makeBalance(2, myMemberId, 500), makeBalance(3, 4, 9999)]

    expect(getPersonalBalance(balances, myMemberId, 'TWD').personalNet).toBe(500)
  })

  it('收付相抵時個人淨額是 0，但還有兩兩淨額就不算已結清', () => {
    const balances = [makeBalance(2, myMemberId, 600), makeBalance(myMemberId, 3, 600)]

    expect(getPersonalBalance(balances, myMemberId, 'TWD')).toEqual({
      personalNet: 0,
      isSettled: false,
    })
  })

  it('只剩其他成員之間的兩兩淨額時，你已結清', () => {
    const balances = [makeBalance(3, 4, 9999)]

    expect(getPersonalBalance(balances, myMemberId, 'TWD')).toEqual({
      personalNet: 0,
      isSettled: true,
    })
  })

  it('沒有任何兩兩淨額時已結清', () => {
    expect(getPersonalBalance([], myMemberId, 'TWD')).toEqual({ personalNet: 0, isSettled: true })
  })

  it('美元相加不會出現小數誤差', () => {
    const balances = [makeBalance(2, myMemberId, 0.1), makeBalance(3, myMemberId, 0.2)]

    expect(getPersonalBalance(balances, myMemberId, 'USD').personalNet).toBe(0.3)
  })

  it('美元剛好相抵時是 0，不會留下極小的零頭', () => {
    const balances = [
      makeBalance(2, myMemberId, 0.1),
      makeBalance(3, myMemberId, 0.2),
      makeBalance(myMemberId, 4, 0.3),
    ]

    expect(getPersonalBalance(balances, myMemberId, 'USD').personalNet).toBe(0)
  })
})

describe('getUnsettledCounterpartIds', () => {
  it('不論欠錢或收錢，對方都算還有帳沒清', () => {
    const balances = [makeBalance(2, 4, 300), makeBalance(5, 2, 120)]

    expect(getUnsettledCounterpartIds(balances, 2)).toEqual([4, 5])
  })

  it('不列出跟這位成員無關的兩兩淨額', () => {
    const balances = [makeBalance(2, 4, 300), makeBalance(3, 5, 9999)]

    expect(getUnsettledCounterpartIds(balances, 2)).toEqual([4])
  })

  it('收付相抵仍算還有帳沒清，兩位對象都要列出', () => {
    const balances = [makeBalance(2, 4, 600), makeBalance(3, 2, 600)]

    expect(getUnsettledCounterpartIds(balances, 2)).toEqual([4, 3])
  })

  it('沒有任何一組牽涉到這位成員時是空陣列', () => {
    expect(getUnsettledCounterpartIds([makeBalance(3, 5, 9999)], 2)).toEqual([])
  })
})
