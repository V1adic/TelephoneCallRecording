<template>
  <AppShell title="Управление звонком" @logout="logout">
    <section class="call-stage">
      <article class="call-stage__hero">
        <p class="eyebrow">Сценарий звонка</p>
        <h2>Запуск, завершение и тарификация в одном экране</h2>
        <p>
          Источник вызова берётся с сервера из привязанного абонента. Пользователь указывает только номер получателя.
        </p>
      </article>

      <form class="call-stage__form" @submit.prevent="submit">
        <MessageBanner
          v-if="message"
          :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
          :message="message"
          :tone="messageTone"
        />

        <label class="field">
          <span>Номер получателя</span>
          <input v-model.trim="destPhone" required maxlength="20" placeholder="+48123456789" />
        </label>

        <div class="call-stage__actions">
          <button class="primary-button" :disabled="callStore.isBusy || hasActiveOtherCall">
            {{ callStore.activeCall ? 'Завершить звонок' : 'Начать звонок' }}
          </button>

          <RouterLink class="text-link" to="/dashboard">Назад в кабинет</RouterLink>
        </div>

        <p v-if="hasActiveOtherCall" class="hint">
          Сначала завершите активный звонок на номер {{ callStore.activeCall?.destPhone }}.
        </p>
      </form>

      <article class="workspace-panel">
        <p class="eyebrow">Текущий статус</p>
        <template v-if="callStore.activeCall">
          <h3>{{ callStore.activeCall.destPhone }}</h3>
          <p>
            Звонок активен с {{ formatDate(callStore.activeCall.startedAtUtc) }} · {{ callStore.activeCall.timeOfDay }}
          </p>
        </template>
        <template v-else>
          <h3>Свободная линия</h3>
          <p>Активных вызовов нет. Можно открыть новый звонок на любой существующий номер получателя.</p>
        </template>
      </article>

      <article v-if="callStore.lastCompletedCall" class="workspace-panel workspace-panel--accent">
        <p class="eyebrow">Итог расчёта</p>
        <h3>{{ callStore.lastCompletedCall.cost.toFixed(2) }} ₽</h3>
        <p>
          {{ callStore.lastCompletedCall.durationMinutes }} мин ·
          скидка {{ callStore.lastCompletedCall.discountPercent.toFixed(2) }}% ·
          базовая стоимость {{ callStore.lastCompletedCall.baseCost.toFixed(2) }} ₽
        </p>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import { ApiError } from '@/lib/api'
import { useAuthStore } from '@/stores/auth'
import { useCallStore } from '@/stores/call'

const authStore = useAuthStore()
const callStore = useCallStore()
const router = useRouter()

const destPhone = ref('')
const message = ref('')
const messageTone = ref<'success' | 'danger'>('success')

const hasActiveOtherCall = computed(() => {
  return Boolean(callStore.activeCall && callStore.activeCall.destPhone !== destPhone.value)
})

onMounted(async () => {
  await callStore.loadActiveCall()
  if (callStore.activeCall) {
    destPhone.value = callStore.activeCall.destPhone
  }
})

watch(
  () => callStore.activeCall,
  (next) => {
    if (next) {
      destPhone.value = next.destPhone
    }
  }
)

async function submit() {
  message.value = ''

  try {
    if (callStore.activeCall) {
      const response = await callStore.finishCall(destPhone.value)
      message.value = response.message
      messageTone.value = 'success'
      return
    }

    const response = await callStore.beginCall(destPhone.value)
    message.value = response.message
    messageTone.value = 'success'
  } catch (cause) {
    message.value = cause instanceof ApiError ? cause.message : 'Не удалось обработать звонок.'
    messageTone.value = 'danger'
  }
}

async function logout() {
  await authStore.signOut()
  await router.push('/login')
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}
</script>
