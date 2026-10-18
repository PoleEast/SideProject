import { describe, expect, it } from 'vitest'

import { sortForHome } from '@/lib/homeGroups'
import type { GroupListItemResponse } from '@/types/group'

const makeGroup = (
  id: number,
  isSettled: boolean,
  lastActivityAt: string,
): GroupListItemResponse => ({
  id,
  name: `群組 ${id}`,
  icon: 'Other',
  baseCurrency: 'TWD',
  closedAt: null,
  activeMemberCount: 3,
  personalNet: isSettled ? 0 : 600,
  isSettled,
  isEveryoneSettled: isSettled,
  lastActivityAt,
})

const idsOf = (groups: GroupListItemResponse[]) => groups.map((group) => group.id)

describe('sortForHome', () => {
  it('還有帳的排在已結清前面，即使已結清的動態比較新', () => {
    const settledButRecent = makeGroup(1, true, '2026-10-16T12:00:00+00:00')
    const unsettledButOld = makeGroup(2, false, '2026-10-01T12:00:00+00:00')

    expect(idsOf(sortForHome([settledButRecent, unsettledButOld]))).toEqual([2, 1])
  })

  it('同一區內依最近一則動態的時間由新到舊', () => {
    const oldest = makeGroup(1, false, '2026-10-14T08:00:00+00:00')
    const newest = makeGroup(2, false, '2026-10-16T08:00:00+00:00')
    const middle = makeGroup(3, false, '2026-10-15T08:00:00+00:00')

    expect(idsOf(sortForHome([oldest, newest, middle]))).toEqual([2, 3, 1])
  })

  it('還有帳與已結清兩區各自依動態時間排序', () => {
    const groups = [
      makeGroup(1, true, '2026-10-10T08:00:00+00:00'),
      makeGroup(2, false, '2026-10-11T08:00:00+00:00'),
      makeGroup(3, true, '2026-10-12T08:00:00+00:00'),
      makeGroup(4, false, '2026-10-13T08:00:00+00:00'),
    ]

    expect(idsOf(sortForHome(groups))).toEqual([4, 2, 3, 1])
  })

  it('時間的時區偏移不同時，依實際的先後排序', () => {
    // 台灣時間 09:00 是 UTC 01:00，比 UTC 02:00 早；照字串比較會排反
    const taipeiNine = makeGroup(1, false, '2026-10-16T09:00:00+08:00')
    const utcTwo = makeGroup(2, false, '2026-10-16T02:00:00+00:00')

    expect(idsOf(sortForHome([taipeiNine, utcTwo]))).toEqual([2, 1])
  })

  it('不改動傳入的陣列', () => {
    const groups = [
      makeGroup(1, false, '2026-10-14T08:00:00+00:00'),
      makeGroup(2, false, '2026-10-16T08:00:00+00:00'),
    ]

    sortForHome(groups)

    expect(idsOf(groups)).toEqual([1, 2])
  })
})
