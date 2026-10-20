import { Bed, Shapes, ShoppingBag, Ticket, TramFront, Utensils } from '@lucide/vue'

import type { TileIcon } from '@/lib/iconTile'
import type { ExpenseCategoryType } from '@/types/expense'

export const expenseCategories: Record<ExpenseCategoryType, TileIcon> = {
  Food: { label: '餐飲', icon: Utensils, color: 'amber' },
  Transport: { label: '交通', icon: TramFront, color: 'blue' },
  Accommodation: { label: '住宿', icon: Bed, color: 'violet' },
  Shopping: { label: '購物', icon: ShoppingBag, color: 'pink' },
  Entertainment: { label: '娛樂', icon: Ticket, color: 'cyan' },
  Other: { label: '其他', icon: Shapes, color: 'stone' },
}
