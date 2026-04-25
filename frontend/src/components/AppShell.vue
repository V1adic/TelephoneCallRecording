<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto flex min-h-screen max-w-7xl flex-col px-4 pb-10 pt-6 sm:px-6 lg:px-8">
      <header class="panel-surface animate-rise mb-6 rounded-[2rem] px-5 py-5 sm:px-6">
        <div class="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
          <div class="flex items-start gap-4">
            <div class="flex h-12 w-12 items-center justify-center rounded-2xl border border-white/10 bg-white/8 text-sm font-semibold tracking-[0.2em] text-cyan-100">
              TC
            </div>
            <div>
              <p class="text-[0.72rem] uppercase tracking-[0.34em] text-cyan-200/75">
                Protected Billing Workspace
              </p>
              <h1 class="mt-2 text-2xl font-semibold tracking-tight text-white sm:text-3xl">
                {{ title }}
              </h1>
              <p v-if="description" class="mt-2 max-w-2xl text-sm leading-6 text-slate-300">
                {{ description }}
              </p>
            </div>
          </div>

          <div class="flex flex-col gap-4 lg:items-end">
            <div class="flex flex-wrap gap-2">
              <span class="badge-shell text-xs">
                {{ roleLabel }}
              </span>
              <span class="badge-shell text-xs">
                {{ authStore.profile?.email ?? 'Сессия активна' }}
              </span>
              <span
                v-if="authStore.profile && !authStore.profile.isEmailConfirmed"
                class="badge-shell border-amber-300/25 bg-amber-300/10 text-xs text-amber-100"
              >
                Email не подтверждён
              </span>
            </div>

            <nav class="flex flex-wrap items-center gap-2">
              <RouterLink
                v-for="item in navigation"
                :key="item.to"
                :to="item.to"
                :class="linkClass(item.to)"
              >
                {{ item.label }}
              </RouterLink>

              <button class="btn-secondary" type="button" @click="signOut">
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
import { useAuthStore } from '@/stores/auth'

defineProps<{
  title: string
  description?: string
}>()

const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const navigation = computed(() => {
  const items = [
    { to: '/dashboard', label: 'Кабинет' },
    { to: '/calls', label: 'Звонки' }
  ]

  if (authStore.profile?.role === 'Admin') {
    items.push({ to: '/admin', label: 'Администрирование' })
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
