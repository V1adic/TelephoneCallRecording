import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'
import './tailwind.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const authStore = useAuthStore(pinia)

router.beforeEach(async (to) => {
  if (!authStore.isBootstrapped) {
    await authStore.bootstrap()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return {
      path: '/login',
      query: { redirect: to.fullPath }
    }
  }

  if (to.meta.adminOnly && authStore.profile?.role !== 'Admin') {
    return '/dashboard'
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return authStore.profile?.role === 'Admin' ? '/admin' : '/dashboard'
  }

  return true
})

authStore
  .bootstrap()
  .catch((error) => {
    console.error('Failed to bootstrap application', error)
  })
  .finally(() => {
    app.mount('#app')
  })
