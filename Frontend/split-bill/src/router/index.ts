import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
    },
    {
      path: '/join/:inviteCode',
      name: 'join-group',
      component: () => import('@/views/HomeView.vue'),
    },
    {
      path: '/quick',
      name: 'quick-expense',
      component: () => import('@/views/QuickExpenseView.vue'),
    },
    {
      path: '/groups/closed',
      name: 'closed-groups',
      component: () => import('@/views/ClosedGroupsView.vue'),
    },
    {
      path: '/groups/:groupId',
      name: 'group',
      component: () => import('@/views/GroupView.vue'),
    },
    {
      path: '/groups/:groupId/expenses/new',
      name: 'expense-new',
      component: () => import('@/views/ExpenseView.vue'),
    },
    {
      path: '/groups/:groupId/expenses/:expenseId',
      name: 'expense-edit',
      component: () => import('@/views/ExpenseView.vue'),
    },
    {
      path: '/groups/:groupId/settlement',
      name: 'settlement',
      component: () => import('@/views/SettlementView.vue'),
    },
    {
      path: '/groups/:groupId/members',
      name: 'members',
      component: () => import('@/views/MembersView.vue'),
    },
    {
      path: '/groups/:groupId/activities',
      name: 'activities',
      component: () => import('@/views/ActivitiesView.vue'),
    },
  ],
})

export default router
