<template>
  <AppShell
    title="Личный кабинет"
    description="Краткая сводка по номеру, активному звонку и начислениям за выбранный период."
  >
    <div class="grid gap-6 lg:grid-cols-[1.3fr_0.7fr]">
      <section class="panel-strong animate-rise rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p class="text-sm font-bold text-blue-700">Добро пожаловать</p>
            <h2 class="mt-3 text-4xl font-black tracking-tight text-slate-950 sm:text-5xl">
              {{ authStore.profile?.username }}
            </h2>
            <p class="mt-4 max-w-2xl text-base leading-7 text-slate-600">
              {{ authStore.profile?.email }} · {{ authStore.profile?.city ?? 'Город не указан' }}
            </p>
          </div>

          <div class="grid gap-3 sm:grid-cols-2">
            <RouterLink class="btn-primary" to="/calls">
              <UiIcon name="phone" :size="18" />
              Управлять звонком
            </RouterLink>
            <RouterLink v-if="authStore.profile?.role === 'Admin'" class="btn-secondary" to="/admin">
              <UiIcon name="shield" :size="18" />
              Администрирование
            </RouterLink>
          </div>
        </div>

        <div class="mt-8 grid gap-4 md:grid-cols-3">
          <article class="metric-card p-5">
            <div class="icon-box">
              <UiIcon name="user" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Роль</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ formatRole(authStore.profile?.role ?? 'Client') }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Доступные разделы подобраны под вашу роль.
            </p>
          </article>

          <article class="metric-card p-5">
            <div class="icon-box green">
              <UiIcon name="phoneCall" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Линия</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ authStore.profile?.phoneNumber ?? 'Не привязана' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Этот номер используется для учёта звонков.
            </p>
          </article>

          <article class="metric-card p-5">
            <div :class="['icon-box', callStore.activeCall ? 'amber' : 'green']">
              <UiIcon name="clock" :size="20" />
            </div>
            <p class="mt-4 text-sm font-bold text-slate-500">Активный звонок</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">
              {{ callStore.activeCall?.destPhone ?? 'Нет активного вызова' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              {{ callStore.activeCall ? `Начат ${formatDateTime(callStore.activeCall.startedAtUtc)}` : 'Можно начать новый звонок.' }}
            </p>
          </article>
        </div>
      </section>

      <section class="panel-surface rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm font-bold text-blue-700">Период</p>
            <h3 class="mt-2 text-2xl font-black text-slate-950">Сводка начислений</h3>
          </div>
          <button class="btn-secondary" type="button" :disabled="isLoading" @click="loadInsights">
            <UiIcon name="refresh" :size="18" />
            {{ isLoading ? 'Обновляем...' : 'Обновить' }}
          </button>
        </div>

        <div class="mt-6 grid gap-4">
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">С</span>
            <input v-model="period.from" class="input-shell" type="date" />
          </label>

          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">По</span>
            <input v-model="period.to" class="input-shell" type="date" />
          </label>
        </div>

        <div class="mt-6 grid gap-4 sm:grid-cols-2">
          <article class="metric-card p-4">
            <p class="text-sm font-bold text-slate-500">Звонки</p>
            <p class="mt-2 text-3xl font-black text-slate-950">{{ summary?.totalCalls ?? 0 }}</p>
          </article>

          <article class="metric-card p-4">
            <p class="text-sm font-bold text-slate-500">Минуты</p>
            <p class="mt-2 text-3xl font-black text-slate-950">{{ summary?.totalMinutes ?? 0 }}</p>
          </article>

          <article class="metric-card p-4">
            <p class="text-sm font-bold text-slate-500">До скидки</p>
            <p class="mt-2 text-3xl font-black text-slate-950">{{ formatCurrency(summary?.baseCost ?? 0) }}</p>
          </article>

          <article class="metric-card p-4">
            <p class="text-sm font-bold text-slate-500">К оплате</p>
            <p class="mt-2 text-3xl font-black text-slate-950">{{ formatCurrency(summary?.totalCost ?? 0) }}</p>
          </article>
        </div>
      </section>
    </div>

    <MessageBanner
      v-if="message"
      class="mt-6"
      :label="messageTone === 'danger' ? 'Ошибка загрузки' : 'Статус'"
      :message="message"
      :tone="messageTone"
    />

    <section class="panel-surface mt-6 rounded-[1.75rem] px-6 py-6 sm:px-8">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p class="text-sm font-bold text-blue-700">История</p>
          <h3 class="mt-2 text-2xl font-black text-slate-950">Последние звонки</h3>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            Недавние вызовы и итоговые суммы за выбранный период.
          </p>
        </div>

        <div class="badge-shell text-xs">
          {{ recentHistory.length }} записей на экране
        </div>
      </div>

      <div v-if="recentHistory.length" class="table-shell soft-scroll mt-6 overflow-x-auto">
        <table class="data-table min-w-[960px]">
          <thead>
            <tr>
              <th>Получатель</th>
              <th>Город</th>
              <th>Старт</th>
              <th>Тариф</th>
              <th>Минуты</th>
              <th>Скидка</th>
              <th>Итог</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in recentHistory" :key="item.callId">
              <td class="font-bold text-slate-950">{{ item.destPhone }}</td>
              <td>{{ item.destinationCity }}</td>
              <td>{{ formatDateTime(item.startedAtUtc) }}</td>
              <td>{{ formatTimeOfDay(item.timeOfDay) }} · {{ formatCurrency(item.appliedTariff) }}</td>
              <td>{{ item.durationMinutes }}</td>
              <td>{{ item.discountPercent.toFixed(2) }}%</td>
              <td class="font-bold text-slate-950">{{ formatCurrency(item.finalCost) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else class="mt-6 rounded-[1.5rem] border border-dashed border-slate-300 bg-slate-50 px-5 py-10 text-center">
        <div class="icon-box mx-auto">
          <UiIcon name="history" :size="22" />
        </div>
        <p class="mt-4 text-lg font-black text-slate-950">История пока пуста</p>
        <p class="mt-3 text-sm leading-6 text-slate-600">
          Завершите хотя бы один звонок, и здесь появятся детализация и начисления.
        </p>
      </div>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import UiIcon from '@/components/UiIcon.vue'
import { ApiError, fetchCallHistory, fetchCallSummary, type CallHistoryItem, type CallSummary } from '@/lib/api'
import { createDefaultPeriod, formatCurrency, formatDateTime, formatRole, formatTimeOfDay, periodToUtc } from '@/lib/format'
import { useAuthStore } from '@/stores/auth'
import { useCallStore } from '@/stores/call'

const authStore = useAuthStore()
const callStore = useCallStore()

const period = ref(createDefaultPeriod())
const summary = ref<CallSummary | null>(null)
const history = ref<CallHistoryItem[]>([])
const isLoading = ref(false)
const message = ref('')
const messageTone = ref<'info' | 'success' | 'danger'>('info')

const recentHistory = computed(() => history.value.slice(0, 8))

async function loadInsights() {
  isLoading.value = true
  message.value = ''

  try {
    const { fromUtc, toUtc } = periodToUtc(period.value)
    const [nextSummary, nextHistory] = await Promise.all([
      fetchCallSummary(fromUtc, toUtc),
      fetchCallHistory(fromUtc, toUtc)
    ])

    summary.value = nextSummary
    history.value = nextHistory
  } catch (error) {
    message.value = error instanceof ApiError ? error.message : 'Не удалось загрузить сводку по звонкам.'
    messageTone.value = 'danger'
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  await Promise.allSettled([
    callStore.loadActiveCall(),
    loadInsights()
  ])
})
</script>
