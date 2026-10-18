import type { CurrencyType } from '@/types/currency'

export type GroupIconType =
  | 'Travel'
  | 'Home'
  | 'Dining'
  | 'Event'
  | 'Sports'
  | 'Party'
  | 'Shopping'
  | 'Other'

export interface GroupListItemResponse {
  id: number
  name: string
  icon: GroupIconType
  baseCurrency: CurrencyType
  closedAt: string | null
  activeMemberCount: number
  personalNet: number
  isSettled: boolean
  isEveryoneSettled: boolean
  lastActivityAt: string
}

export interface GroupResponse {
  id: number
  name: string
  description: string
  icon: GroupIconType
  baseCurrency: CurrencyType
  inviteCode: string
  closedAt: string | null
  createdAt: string
}

export interface SetGroupClosedRequest {
  closed: boolean
}
