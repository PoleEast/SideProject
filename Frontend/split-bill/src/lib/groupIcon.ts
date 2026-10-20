import {
  House,
  PartyPopper,
  Plane,
  Shapes,
  ShoppingBag,
  Ticket,
  Utensils,
  Volleyball,
} from '@lucide/vue'

import type { TileIcon } from '@/lib/iconTile'
import type { GroupIconType } from '@/types/group'

export const groupIcons: Record<GroupIconType, TileIcon> = {
  Travel: { label: '旅行', icon: Plane, color: 'blue' },
  Home: { label: '住家', icon: House, color: 'violet' },
  Dining: { label: '聚餐', icon: Utensils, color: 'amber' },
  Event: { label: '活動', icon: Ticket, color: 'cyan' },
  Sports: { label: '運動', icon: Volleyball, color: 'lime' },
  Party: { label: '派對', icon: PartyPopper, color: 'orange' },
  Shopping: { label: '購物', icon: ShoppingBag, color: 'pink' },
  Other: { label: '其他', icon: Shapes, color: 'stone' },
}
