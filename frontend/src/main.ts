import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'
import './styles.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const authStore = useAuthStore(pinia)

router.beforeEach(async (to) => {
  if (!authStore.isBootstrapped && !authStore.isBootstrapping) {
    await authStore.bootstrap()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return {
      path: '/login',
      query: { redirect: to.fullPath }
    }
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return '/dashboard'
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
