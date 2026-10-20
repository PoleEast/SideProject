import type { GroupMemberResponse } from '@/types/groupMember'

const otherMemberColorClasses = [
  'bg-avatar-mint',
  'bg-avatar-lime',
  'bg-avatar-lavender',
  'bg-avatar-pink',
  'bg-avatar-sky',
  'bg-avatar-peach',
]

// 中文名字取最後一個字，否則取第一個字母並轉成大寫
export const getInitial = (name: string): string => {
  const characters = Array.from(name.trim())
  const hanCharacters = characters.filter((character) => /\p{Script=Han}/u.test(character))

  if (hanCharacters.length > 0) {
    return hanCharacters[hanCharacters.length - 1] ?? ''
  }

  return (characters[0] ?? '').toUpperCase()
}

// 你固定一個顏色，其他人依成員在列表裡的先後輪流
export const getAvatarColorClass = (members: GroupMemberResponse[], memberId: number): string => {
  const otherMembers = members.filter((member) => !member.isMe)
  const index = otherMembers.findIndex((member) => member.id === memberId)
  if (index === -1) {
    return 'bg-avatar-me'
  }
  return otherMemberColorClasses[index % otherMemberColorClasses.length] ?? 'bg-avatar-me'
}
