<template>
  <AppShell
    title="Управление звонком"
    description="Запуск и завершение вызова выполняются в одном интерфейсе. Источник определяется сервером, а пользователь указывает только номер получателя."
  >
    <div class="grid gap-6 lg:grid-cols-[1.15fr_0.85fr]">
      <section class="panel-strong animate-rise rounded-[2rem] px-6 py-6 sm:px-8">
        <p class="text-xs uppercase tracking-[0.34em] text-cyan-200/80">Call Scenario</p>
        <h2 class="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
          Один экран для старта, завершения и тарификации
        </h2>
        <p class="mt-5 max-w-3xl text-base leading-7 text-slate-300">
          Система не позволяет начать второй активный звонок, пока не завершён текущий. Итоговая стоимость
          рассчитывается на сервере по времени суток, тарифу города и подходящей скидке.
        </p>

        <div class="mt-8 grid gap-4 md:grid-cols-3">
          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Статус линии</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ callStore.activeCall ? 'Занята' : 'Свободна' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              {{ callStore.activeCall ? `Активный получатель: ${callStore.activeCall.destPhone}` : 'Можно запускать новый вызов.' }}
            </p>
          </article>

          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Время запуска</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ callStore.activeCall ? formatDateTime(callStore.activeCall.startedAtUtc) : '—' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              {{ callStore.activeCall ? `Тариф: ${formatTimeOfDay(callStore.activeCall.timeOfDay)}` : 'Время тарифа будет зафиксировано при старте.' }}
            </p>
          </article>

          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Последний расчёт</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ callStore.lastCompletedCall ? formatCurrency(callStore.lastCompletedCall.cost) : '—' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              {{ lastCallCopy }}
            </p>
          </article>
        </div>
      </section>

      <section class="panel-surface rounded-[2rem] px-6 py-6 sm:px-8">
        <div class="space-y-6">
          <div>
            <p class="text-xs uppercase tracking-[0.3em] text-cyan-200/80">Control Form</p>
            <h3 class="mt-3 text-2xl font-semibold text-white">Номер получателя</h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              Используйте номер зарегистрированного абонента в международном формате.
            </p>
          </div>

          <MessageBanner
            v-if="message"
            :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
            :message="message"
            :tone="messageTone"
          />

          <form class="space-y-5" @submit.prevent="submit">
            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Получатель</span>
              <input
                v-model.trim="destPhone"
                class="input-shell"
                required
                maxlength="20"
                placeholder="+48123456789"
              />
            </label>

            <div class="rounded-[1.4rem] border border-white/10 bg-white/4 p-4 text-sm leading-6 text-slate-300">
              Если сейчас активен другой звонок, завершите его тем же номером, который уже отображается в статусе линии.
            </div>

            <button class="btn-primary w-full" :disabled="callStore.isBusy || hasActiveOtherCall">
              {{ callStore.activeCall ? 'Завершить звонок' : 'Начать звонок' }}
            </button>

            <RouterLink class="inline-flex text-sm font-semibold text-cyan-200 transition hover:text-white" to="/dashboard">
              Вернуться в кабинет
            </RouterLink>

            <p v-if="hasActiveOtherCall" class="text-sm font-medium text-amber-200">
              Активен звонок на номер {{ callStore.activeCall?.destPhone }}. Сначала завершите его.
            </p>
          </form>
        </div>
      </section>
    </div>
  </AppShell>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import { ApiError } from '@/lib/api'
import { formatCurrency, formatDateTime, formatTimeOfDay } from '@/lib/format'
import { useCallStore } from '@/stores/call'

const callStore = useCallStore()

const destPhone = ref('')
const message = ref('')
const messageTone = ref<'success' | 'danger'>('success')

const hasActiveOtherCall = computed(() => {
  return Boolean(callStore.activeCall && callStore.activeCall.destPhone !== destPhone.value)
})

const lastCallCopy = computed(() => {
  if (!callStore.lastCompletedCall) {
    return 'Стоимость и скидка появятся после завершения вызова.'
  }

  return `${callStore.lastCompletedCall.durationMinutes} мин · скидка ${callStore.lastCompletedCall.discountPercent.toFixed(2)}%`
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
</script>
