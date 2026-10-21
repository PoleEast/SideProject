import type {
  AddGroupMembersRequest,
  GroupMemberResponse,
  RenameGroupMemberRequest,
} from '@/types/groupMember'

import { request } from './helper'

export const getGroupMembers = (groupId: number) =>
  request<GroupMemberResponse[]>(`/group/${groupId}/members`, 'GET')

export const addGroupMembers = (groupId: number, requestBody: AddGroupMembersRequest) =>
  request<GroupMemberResponse[]>(`/group/${groupId}/members`, 'POST', requestBody)

export const renameGroupMember = (
  groupId: number,
  memberId: number,
  requestBody: RenameGroupMemberRequest,
) => request<GroupMemberResponse>(`/group/${groupId}/members/${memberId}`, 'PUT', requestBody)

export const removeGroupMember = (groupId: number, memberId: number) =>
  request<null>(`/group/${groupId}/members/${memberId}`, 'DELETE')

export const unbindGroupMember = (groupId: number, memberId: number) =>
  request<null>(`/group/${groupId}/members/${memberId}/binding`, 'DELETE')
