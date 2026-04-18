<template>
  <AuthShell
    title="Операционный вход"
    lead="Вход для сотрудника или абонента открывает защищённый кабинет, где уже доступны запуск звонка, расчёт стоимости и текущий статус сессии."
  >
    <form class="stack" @submit.prevent="submit">
      <div class="form-copy">
        <h2>Войти в систему</h2>
        <p>Используйте те же учётные данные, с которыми проходили регистрацию.</p>
      </div>

      <MessageBanner
        v-if="error"
        label="Ошибка"
        :message="error"
        tone="danger"
      />

      <label class="field">
        <span>Username</span>
        <input v-model.trim="form.username" autocomplete="username" required maxlength="15" minlength="5" />
      </label>

      <label class="field">
        <span>Password</span>
        <input v-model="form.password" type="password" autocomplete="current-password" required minlength="12" maxlength="100" />
      </label>

      <button class="primary-button" :disabled="isSubmitting">
        {{ isSubmitting ? 'Выполняем вход...' : 'Войти' }}
      </button>

      <RouterLink class="text-link" to="/register">Нет аккаунта? Зарегистрироваться</RouterLink>
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
