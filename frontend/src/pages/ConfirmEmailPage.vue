<template>
  <AuthShell
    title="Подтверждение email"
    lead="Verification-cookie хранит короткую сессию подтверждения. Если код истёк, можно тут же запросить новый, не выходя из сценария."
  >
    <form class="stack" @submit.prevent="submit">
      <div class="form-copy">
        <h2>Введите код</h2>
        <p>Код действует 15 минут. После подтверждения можно сразу вернуться к входу.</p>
      </div>

      <MessageBanner
        v-if="message"
        :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
        :message="message"
        :tone="messageTone"
      />

      <label class="field">
        <span>Confirmation code</span>
        <input v-model.trim="code" required maxlength="20" />
      </label>

      <button class="primary-button" :disabled="isSubmitting">
        {{ isSubmitting ? 'Проверяем...' : 'Подтвердить email' }}
      </button>

      <button class="ghost-button" type="button" :disabled="isSubmitting || isResending" @click="resend">
        {{ isResending ? 'Отправляем новый код...' : 'Запросить новый код' }}
      </button>

      <RouterLink class="text-link" to="/login">Вернуться ко входу</RouterLink>
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
