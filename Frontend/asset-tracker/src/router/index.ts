import PnlView from '@/views/PnlView.vue'
import PositionsView from '@/views/PositionsView.vue'
import TransactionsView from '@/views/TransactionsView.vue'
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/positions',
      component: PositionsView,
    },
    {
      path: '/pnl',
      component: PnlView,
    },
    {
      path: '/transactions',
      component: TransactionsView,
    },
    {
      path: '/',
      redirect: '/positions',
    },
  ],
})

export default router
