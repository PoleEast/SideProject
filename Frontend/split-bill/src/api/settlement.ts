import type { MemberBalance } from '@/types/settlement'

import { request } from './helper'

export const getBalances = (groupId: number) =>
  request<MemberBalance[]>(`/group/${groupId}/balances`, 'GET')
