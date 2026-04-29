<template>
  <AuthShell
    title="Войдите, чтобы продолжить работу"
    lead="Личный кабинет показывает активные звонки, историю и начисления. Администраторы получают дополнительные инструменты для тарифов и отчётов."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs">
          <UiIcon name="lock" :size="15" />
          Вход в аккаунт
        </span>
        <div>
          <h2 class="text-3xl font-black tracking-tight text-slate-950 sm:text-[2.2rem]">
            Войти в систему
          </h2>
          <p class="mt-3 max-w-xl text-sm leading-6 text-slate-600">
            Используйте имя пользователя и пароль. После входа откроется ваш рабочий кабинет.
          </p>
        </div>
      </div>

      <MessageBanner
        v-if="error"
        label="Ошибка"
        :message="error"
        tone="danger"
      />

      <label class="block space-y-2">
        <span class="text-sm font-bold text-slate-700">Имя пользователя</span>
        <input
          v-model.trim="form.username"
          class="input-shell"
          autocomplete="username"
          required
          maxlength="15"
          minlength="5"
        />
      </label>

      <label class="block space-y-2">
        <span class="text-sm font-bold text-slate-700">Пароль</span>
        <input
          v-model="form.password"
          class="input-shell"
          type="password"
          autocomplete="current-password"
          required
          minlength="12"
          maxlength="100"
        />
      </label>

      <button class="btn-primary w-full" :disabled="isSubmitting">
        <UiIcon name="logOut" :size="18" />
        {{ isSubmitting ? 'Проверяем данные...' : 'Войти' }}
      </button>

      <div class="flex flex-col gap-3 text-sm text-slate-600 sm:flex-row sm:items-center sm:justify-between">
        <span>Нет аккаунта?</span>
        <RouterLink class="font-bold text-blue-700 transition hover:text-blue-900" to="/register">
          Зарегистрироваться
        </RouterLink>
      </div>
    </form>
  </AuthShell>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AuthShell from '@/components/AuthShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import UiIcon from '@/components/UiIcon.vue'
import { ApiError } from '@/lib/api'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const form = reactive({
  username: '',
  password: ''
})

const isSubmitting = ref(false)
const error = ref('')

async function submit() {
  isSubmitting.value = true
  error.value = ''

  try {
    await authStore.signIn(form)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/dashboard'
    await router.push(redirect)
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось выполнить вход.'
  } finally {
    isSubmitting.value = false
  }
}
</script>
