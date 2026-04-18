import { createRouter, createWebHistory } from 'vue-router'
import LoginPage from '@/pages/LoginPage.vue'
import RegisterPage from '@/pages/RegisterPage.vue'
import ConfirmEmailPage from '@/pages/ConfirmEmailPage.vue'
import DashboardPage from '@/pages/DashboardPage.vue'
import CallSessionPage from '@/pages/CallSessionPage.vue'
import NotFoundPage from '@/pages/NotFoundPage.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard'
    },
    {
      path: '/login',
      component: LoginPage,
      meta: { guestOnly: true }
    },
    {
      path: '/register',
      component: RegisterPage,
      meta: { guestOnly: true }
    },
    {
      path: '/confirm-email',
      component: ConfirmEmailPage
    },
    {
      path: '/dashboard',
      component: DashboardPage,
      meta: { requiresAuth: true }
    },
    {
      path: '/calls',
      component: CallSessionPage,
      meta: { requiresAuth: true }
    },
    {
      path: '/:pathMatch(.*)*',
      component: NotFoundPage
    }
  ]
})

export default router
