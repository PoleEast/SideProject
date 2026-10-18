import { describe, expect, it } from 'vitest'

import { formatMoney, minorUnitDecimals } from '@/lib/money'

describe('minorUnitDecimals', () => {
  it('各幣別的小數位數與後端一致', () => {
    expect(minorUnitDecimals).toEqual({ TWD: 0, JPY: 0, USD: 2 })
  })
})

describe('formatMoney', () => {
  it('台幣不帶小數並加上千分位', () => {
    expect(formatMoney('TWD', 1250)).toBe('TWD 1,250')
  })

  it('日圓不帶小數並加上千分位', () => {
    expect(formatMoney('JPY', 33400)).toBe('JPY 33,400')
  })

  it('美元整數金額也補滿兩位小數', () => {
    expect(formatMoney('USD', 12)).toBe('USD 12.00')
  })

  it('美元一位小數補成兩位', () => {
    expect(formatMoney('USD', 1234.5)).toBe('USD 1,234.50')
  })

  it('零元照常輸出', () => {
    expect(formatMoney('TWD', 0)).toBe('TWD 0')
  })

  it('金額上限的每一位都保留', () => {
    expect(formatMoney('USD', 999999999.99)).toBe('USD 999,999,999.99')
  })

  it('負數照原樣輸出，正負由呼叫端以文案表達', () => {
    expect(formatMoney('TWD', -1250)).toBe('TWD -1,250')
  })

  it('幣別代碼與數字之間是一般空白', () => {
    expect(formatMoney('JPY', 800).charCodeAt(3)).toBe(0x20)
  })
})
