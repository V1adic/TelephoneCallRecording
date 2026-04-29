<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto flex min-h-screen max-w-7xl flex-col px-4 pb-10 pt-5 sm:px-6 lg:px-8">
      <header class="panel-surface animate-rise mb-6 rounded-[1.75rem] px-4 py-4 sm:px-5">
        <div class="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
          <div class="flex min-w-0 items-start gap-4">
            <div class="icon-box pulse-line">
              <UiIcon name="phoneCall" :size="22" />
            </div>
            <div class="min-w-0">
              <p class="text-sm font-bold text-blue-700">Запись телефонных звонков</p>
              <h1 class="mt-1 text-2xl font-black tracking-tight text-slate-950 sm:text-3xl">
                {{ title }}
              </h1>
              <p v-if="description" class="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
                {{ description }}
              </p>
            </div>
          </div>

          <div class="flex flex-col gap-3 lg:items-end">
            <div class="flex flex-wrap gap-2">
              <span class="badge-shell text-xs">
                <UiIcon name="user" :size="15" />
                {{ roleLabel }}
              </span>
              <span class="badge-shell max-w-full text-xs">
                <UiIcon name="mail" :size="15" />
                <span class="truncate">{{ authStore.profile?.email ?? 'Гость' }}</span>
              </span>
              <span
                v-if="authStore.profile && !authStore.profile.isEmailConfirmed"
                class="badge-shell border-amber-200 bg-amber-50 text-xs text-amber-700"
              >
                <UiIcon name="bell" :size="15" />
                Подтвердите email
              </span>
            </div>

            <nav class="flex flex-wrap items-center gap-2">
              <RouterLink
                v-for="item in navigation"
                :key="item.to"
                :to="item.to"
                :class="linkClass(item.to)"
              >
                <UiIcon :name="item.icon" :size="18" />
                {{ item.label }}
              </RouterLink>

              <button class="btn-secondary" type="button" @click="signOut">
                <UiIcon name="logOut" :size="18" />
                Выйти
              </button>
            </nav>
          </div>
        </div>
      </header>

      <main class="flex-1">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import UiIcon from '@/components/UiIcon.vue'
import { useAuthStore } from '@/stores/auth'

defineProps<{
  title: string
  description?: string
}>()

type NavigationItem = {
  to: string
  label: string
  icon: 'home' | 'phone' | 'shield'
}

const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const navigation = computed(() => {
  const items: NavigationItem[] = [
    { to: '/dashboard', label: 'Кабинет', icon: 'home' },
    { to: '/calls', label: 'Звонки', icon: 'phone' }
  ]

  if (authStore.profile?.role === 'Admin') {
    items.push({ to: '/admin', label: 'Администрирование', icon: 'shield' })
  }

  return items
})

const roleLabel = computed(() => (authStore.profile?.role === 'Admin' ? 'Администратор' : 'Клиент'))

function linkClass(path: string) {
  const isActive = route.path === path || route.path.startsWith(`${path}/`)
  return ['nav-link', isActive ? 'is-active' : '']
}

async function signOut() {
  try {
    await authStore.signOut()
  } finally {
    await router.replace('/login')
  }
}
</script>
