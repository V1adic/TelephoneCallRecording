<template>
  <AuthShell
    title="Безопасный вход"
    lead="Аутентификация проходит по защищённому маршруту: после входа пользователь попадает только в свой контур доступа, а административные функции выдаются строго по роли."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs uppercase tracking-[0.22em] text-cyan-100">
          Session Access
        </span>
        <div>
          <h2 class="text-3xl font-semibold tracking-tight text-white sm:text-[2.2rem]">
            Войти в систему
          </h2>
          <p class="mt-3 max-w-xl text-sm leading-6 text-slate-300">
            Используйте те же учётные данные, с которыми проходили регистрацию. После входа система сразу загрузит профиль,
            активный звонок и доступные разделы.
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
        <span class="text-sm font-medium text-slate-300">Имя пользователя</span>
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
        <span class="text-sm font-medium text-slate-300">Пароль</span>
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
        {{ isSubmitting ? 'Проверяем учётные данные...' : 'Войти в рабочий контур' }}
      </button>

      <div class="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
        <span>Нет аккаунта?</span>
        <RouterLink class="font-semibold text-cyan-200 transition hover:text-white" to="/register">
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
