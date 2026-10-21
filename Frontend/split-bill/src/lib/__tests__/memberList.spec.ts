import { describe, expect, it } from 'vitest'

import { listActiveMembers } from '@/lib/memberList'
import type { GroupMemberResponse } from '@/types/groupMember'

const makeMember = (
  id: number,
  { isMe = false, isRemoved = false }: { isMe?: boolean; isRemoved?: boolean } = {},
): GroupMemberResponse => ({
  id,
  displayName: `成員 ${id}`,
  isRemoved,
  isMe,
  isOwner: id === 1,
  isBound: isMe,
})

const idsOf = (members: GroupMemberResponse[]) => members.map((member) => member.id)

describe('listActiveMembers', () => {
  it('已移除的成員不列出', () => {
    const members = [
      makeMember(1, { isMe: true }),
      makeMember(2, { isRemoved: true }),
      makeMember(3),
    ]

    expect(idsOf(listActiveMembers(members))).toEqual([1, 3])
  })

  it('你排第一，即使你在後端名單的中間', () => {
    const members = [makeMember(1), makeMember(2), makeMember(3, { isMe: true }), makeMember(4)]

    expect(idsOf(listActiveMembers(members))[0]).toBe(3)
  })

  it('你以外的人維持後端的順序', () => {
    const members = [makeMember(4), makeMember(2, { isMe: true }), makeMember(1), makeMember(3)]

    expect(idsOf(listActiveMembers(members))).toEqual([2, 4, 1, 3])
  })
})
