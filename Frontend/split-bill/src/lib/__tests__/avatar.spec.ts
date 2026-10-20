import { describe, expect, it } from 'vitest'

import { getAvatarColorClass, getInitial } from '@/lib/avatar'
import type { GroupMemberResponse } from '@/types/groupMember'

const makeMember = (id: number, { isMe = false, isRemoved = false } = {}): GroupMemberResponse => ({
  id,
  displayName: `成員 ${id}`,
  isRemoved,
  isMe,
  isOwner: false,
  isBound: isMe,
})

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

describe('getAvatarColorClass', () => {
  it('你固定用自己的顏色', () => {
    const members = [makeMember(1, { isMe: true }), makeMember(2)]

    expect(getAvatarColorClass(members, 1)).toBe('bg-avatar-me')
  })

  it('其他人依成員列表的先後輪流配色', () => {
    const members = [makeMember(1, { isMe: true }), makeMember(2), makeMember(3), makeMember(4)]

    expect(getAvatarColorClass(members, 2)).toBe('bg-avatar-mint')
    expect(getAvatarColorClass(members, 3)).toBe('bg-avatar-lime')
    expect(getAvatarColorClass(members, 4)).toBe('bg-avatar-lavender')
  })

  it('你排在中間時，不佔用其他人的顏色', () => {
    const members = [makeMember(1), makeMember(2, { isMe: true }), makeMember(3)]

    expect(getAvatarColorClass(members, 1)).toBe('bg-avatar-mint')
    expect(getAvatarColorClass(members, 3)).toBe('bg-avatar-lime')
  })

  it('第 7 位其他成員回到第一個顏色', () => {
    const members = [
      makeMember(1, { isMe: true }),
      ...[2, 3, 4, 5, 6, 7, 8].map((id) => makeMember(id)),
    ]

    expect(getAvatarColorClass(members, 7)).toBe('bg-avatar-peach')
    expect(getAvatarColorClass(members, 8)).toBe('bg-avatar-mint')
  })

  it('已移除的成員照樣佔一個位置，後面的人顏色不變', () => {
    const members = [
      makeMember(1, { isMe: true }),
      makeMember(2, { isRemoved: true }),
      makeMember(3),
    ]

    expect(getAvatarColorClass(members, 2)).toBe('bg-avatar-mint')
    expect(getAvatarColorClass(members, 3)).toBe('bg-avatar-lime')
  })
})
