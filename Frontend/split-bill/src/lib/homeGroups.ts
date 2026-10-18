import type { GroupListItemResponse } from '@/types/group'

// 還有帳的在上、已結清的在下；同一區內最近有動態的在前
export const sortForHome = (groups: GroupListItemResponse[]): GroupListItemResponse[] =>
  [...groups].sort((a, b) => {
    if (a.isSettled != b.isSettled) {
      return a.isSettled ? 1 : -1
    }
    return Date.parse(b.lastActivityAt) - Date.parse(a.lastActivityAt)
  })
