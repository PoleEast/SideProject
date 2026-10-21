import type {
  CreateGroupRequest,
  GroupListItemResponse,
  GroupResponse,
  SetGroupClosedRequest,
} from '@/types/group'

import { request } from './helper'

export const getMyGroups = () => request<GroupListItemResponse[]>('/group', 'GET')

export const getGroup = (groupId: number) => request<GroupResponse>(`/group/${groupId}`, 'GET')

export const setGroupClosed = (groupId: number, requestBody: SetGroupClosedRequest) =>
  request<GroupResponse>(`/group/${groupId}/closed`, 'PUT', requestBody)

export const createGroup = (requestBody: CreateGroupRequest) =>
  request<GroupResponse>('/group', 'POST', requestBody)

export const resetInviteCode = (groupId: number) =>
  request<GroupResponse>(`/group/${groupId}/invite-code`, 'POST')
