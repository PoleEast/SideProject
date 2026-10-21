export interface GroupMemberResponse {
  id: number
  displayName: string
  isRemoved: boolean
  isMe: boolean
  isOwner: boolean
  isBound: boolean
}

export interface AddGroupMembersRequest {
  displayNames: string[]
}

export interface RenameGroupMemberRequest {
  displayName: string
}
