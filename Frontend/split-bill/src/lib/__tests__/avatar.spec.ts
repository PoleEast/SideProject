import { describe, expect, it } from 'vitest'

import { getInitial } from '@/lib/avatar'

describe('getInitial', () => {
  it('中文名字取最後一個字', () => {
    expect(getInitial('阿凱')).toBe('凱')
    expect(getInitial('王小明')).toBe('明')
  })

  it('英文名字取第一個字母並轉成大寫', () => {
    expect(getInitial('Mia')).toBe('M')
    expect(getInitial('amy chen')).toBe('A')
  })

  it('中英混合時取最後一個中文字', () => {
    expect(getInitial('小明Kevin')).toBe('明')
    expect(getInitial('Kevin 王')).toBe('王')
  })

  it('忽略前後空白', () => {
    expect(getInitial('  小安 ')).toBe('安')
    expect(getInitial(' mia')).toBe('M')
  })

  it('罕用字佔兩個 UTF-16 單位時仍取出完整的一個字', () => {
    // 「𠮷」不在常用字範圍，JS 字串裡要兩個 UTF-16 單位才存得下
    expect(getInitial('田𠮷')).toBe('𠮷')
  })

  it('名字是空字串時回傳空字串', () => {
    expect(getInitial('')).toBe('')
    expect(getInitial('   ')).toBe('')
  })
})
