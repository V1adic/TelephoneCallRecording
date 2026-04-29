<template>
  <AppShell
    title="Управление звонком"
    description="Начните или завершите звонок, а система сохранит историю и рассчитает стоимость."
  >
    <div class="grid gap-6 lg:grid-cols-[1.15fr_0.85fr]">
      <section class="panel-strong animate-rise rounded-[1.75rem] px-6 py-6 sm:px-8">
        <p class="text-sm font-bold text-blue-700">Работа со звонком</p>
        <h2 class="mt-3 text-4xl font-black tracking-tight text-slate-950 sm:text-5xl">
          Один экран для старта и завершения вызова
        </h2>
        <p class="mt-5 max-w-3xl text-base leading-7 text-slate-600">
          Если звонок уже активен, завершите его тем же номером. После завершения стоимость появится в истории и сводке.
        </p>

        <div class="mt-8 grid gap-4 md:grid-cols-3">
          <article class="metric-card p-5">
            <div :class="['icon-box', callStore.activeCall ? 'amber' : 'green']">
              <UiIcon name="phoneCall" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Статус линии</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ callStore.activeCall ? 'Занята' : 'Свободна' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              {{ callStore.activeCall ? `Активный получатель: ${callStore.activeCall.destPhone}` : 'Можно запускать новый вызов.' }}
            </p>
          </article>

          <article class="metric-card p-5">
            <div class="icon-box">
              <UiIcon name="clock" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Время запуска</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ callStore.activeCall ? formatDateTime(callStore.activeCall.startedAtUtc) : '—' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              {{ callStore.activeCall ? `Период: ${formatTimeOfDay(callStore.activeCall.timeOfDay)}` : 'Появится после начала звонка.' }}
            </p>
          </article>

          <article class="metric-card p-5">
            <div class="icon-box green">
              <UiIcon name="wallet" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Последний расчёт</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ callStore.lastCompletedCall ? formatCurrency(callStore.lastCompletedCall.cost) : '—' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              {{ lastCallCopy }}
            </p>
          </article>
        </div>
      </section>

      <section class="panel-surface rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="space-y-6">
          <div>
            <p class="text-sm font-bold text-blue-700">Форма звонка</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">Номер получателя</h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Укажите номер в международном формате.
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
              <span class="text-sm font-bold text-slate-700">Получатель</span>
              <input
                v-model.trim="destPhone"
                class="input-shell"
                required
                maxlength="20"
                placeholder="+48123456789"
              />
            </label>

            <div class="rounded-[1.25rem] border border-blue-100 bg-blue-50 p-4 text-sm leading-6 text-slate-700">
              Если сейчас активен другой звонок, завершите его тем же номером, который уже отображается в статусе линии.
            </div>

            <button class="btn-primary w-full" :disabled="callStore.isBusy || hasActiveOtherCall">
              <UiIcon :name="callStore.activeCall ? 'check' : 'phone'" :size="18" />
              {{ callStore.activeCall ? 'Завершить звонок' : 'Начать звонок' }}
            </button>

            <RouterLink class="inline-flex items-center gap-2 text-sm font-bold text-blue-700 transition hover:text-blue-900" to="/dashboard">
              <UiIcon name="arrowLeft" :size="16" />
              Вернуться в кабинет
            </RouterLink>

            <p v-if="hasActiveOtherCall" class="text-sm font-bold text-amber-700">
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
import UiIcon from '@/components/UiIcon.vue'
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
