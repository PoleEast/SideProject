import type { GroupMemberResponse } from '@/types/groupMember'

// 你排第一，其他人維持後端的順序
export const listActiveMembers = (members: GroupMemberResponse[]): GroupMemberResponse[] => {
  const activeMembers = members.filter((member) => !member.isRemoved)
  return [
    ...activeMembers.filter((member) => member.isMe),
    ...activeMembers.filter((member) => !member.isMe),
  ]
}
