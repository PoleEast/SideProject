import type { GroupMemberResponse } from '@/types/groupMember'

import { request } from './helper'

export const getGroupMembers = (groupId: number) =>
  request<GroupMemberResponse[]>(`/group/${groupId}/members`, 'GET')
