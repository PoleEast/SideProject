import type { ExpenseResponse } from '@/types/expense'

import { request } from './helper'

export const getExpenses = (groupId: number) =>
  request<ExpenseResponse[]>(`/group/${groupId}/expenses`, 'GET')
