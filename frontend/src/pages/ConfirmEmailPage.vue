<template>
  <AuthShell
    title="Подтверждение email"
    lead="Введите код из письма, чтобы завершить регистрацию и перейти к работе в личном кабинете."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs">
          <UiIcon name="mail" :size="15" />
          Проверка email
        </span>
        <div>
          <h2 class="text-3xl font-black tracking-tight text-slate-950 sm:text-[2.2rem]">
            Введите код
          </h2>
          <p class="mt-3 text-sm leading-6 text-slate-600">
            Код действует 15 минут. После успешного подтверждения можно сразу возвращаться ко входу.
          </p>
        </div>
      </div>

      <MessageBanner
        v-if="message"
        :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
        :message="message"
        :tone="messageTone"
      />

      <label class="block space-y-2">
        <span class="text-sm font-bold text-slate-700">Код подтверждения</span>
        <input v-model.trim="code" class="input-shell" required maxlength="20" />
      </label>

      <button class="btn-primary w-full" :disabled="isSubmitting">
        <UiIcon name="check" :size="18" />
        {{ isSubmitting ? 'Проверяем...' : 'Подтвердить email' }}
      </button>

      <button class="btn-secondary w-full" type="button" :disabled="isSubmitting || isResending" @click="resend">
        <UiIcon name="refresh" :size="18" />
        {{ isResending ? 'Отправляем новый код...' : 'Запросить новый код' }}
      </button>

      <RouterLink class="inline-flex items-center gap-2 text-sm font-bold text-blue-700 transition hover:text-blue-900" to="/login">
        <UiIcon name="arrowLeft" :size="16" />
        Вернуться ко входу
      </RouterLink>
    </form>
  </AuthShell>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AuthShell from '@/components/AuthShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import UiIcon from '@/components/UiIcon.vue'
import { ApiError } from '@/lib/api'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()

const code = ref('')
const isSubmitting = ref(false)
const isResending = ref(false)
const message = ref('')
const messageTone = ref<'success' | 'danger'>('success')

async function submit() {
  isSubmitting.value = true
  message.value = ''

  try {
    const response = await authStore.completeConfirmation(code.value)
    message.value = response.message
    messageTone.value = 'success'
    setTimeout(() => {
      router.push('/login')
    }, 600)
  } catch (cause) {
    message.value = cause instanceof ApiError ? cause.message : 'Не удалось подтвердить email.'
    messageTone.value = 'danger'
  } finally {
    isSubmitting.value = false
  }
}

async function resend() {
  isResending.value = true
  message.value = ''

  try {
    const response = await authStore.resendConfirmation()
    message.value = response.message
    messageTone.value = response.code === 'resent_delivery_failed' ? 'danger' : 'success'
  } catch (cause) {
    message.value = cause instanceof ApiError ? cause.message : 'Не удалось запросить новый код.'
    messageTone.value = 'danger'
  } finally {
    isResending.value = false
  }
}
</script>
