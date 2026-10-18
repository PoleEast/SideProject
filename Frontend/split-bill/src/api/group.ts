import type { GroupListItemResponse, GroupResponse, SetGroupClosedRequest } from '@/types/group'

import { request } from './helper'

export const getMyGroups = () => request<GroupListItemResponse[]>('/group', 'GET')

export const setGroupClosed = (groupId: number, requestBody: SetGroupClosedRequest) =>
  request<GroupResponse>(`/group/${groupId}/closed`, 'PUT', requestBody)
