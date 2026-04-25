<template>
  <AuthShell
    title="Подтверждение email"
    lead="Verification-cookie хранит короткую сессию подтверждения. Если код истёк, новый можно запросить здесь же, не теряя текущий защищённый сценарий."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs uppercase tracking-[0.22em] text-cyan-100">
          Email Verification
        </span>
        <div>
          <h2 class="text-3xl font-semibold tracking-tight text-white sm:text-[2.2rem]">
            Введите код
          </h2>
          <p class="mt-3 text-sm leading-6 text-slate-300">
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
        <span class="text-sm font-medium text-slate-300">Код подтверждения</span>
        <input v-model.trim="code" class="input-shell" required maxlength="20" />
      </label>

      <button class="btn-primary w-full" :disabled="isSubmitting">
        {{ isSubmitting ? 'Проверяем...' : 'Подтвердить email' }}
      </button>

      <button class="btn-secondary w-full" type="button" :disabled="isSubmitting || isResending" @click="resend">
        {{ isResending ? 'Отправляем новый код...' : 'Запросить новый код' }}
      </button>

      <RouterLink class="inline-flex text-sm font-semibold text-cyan-200 transition hover:text-white" to="/login">
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
