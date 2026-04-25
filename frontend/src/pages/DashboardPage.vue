<template>
  <AppShell
    title="Операционный кабинет"
    description="Здесь пользователь видит собственные начисления, историю звонков и текущее состояние линии. Администратор дополнительно получает быстрый переход в контур сопровождения."
  >
    <div class="grid gap-6 lg:grid-cols-[1.3fr_0.7fr]">
      <section class="panel-strong animate-rise rounded-[2rem] px-6 py-6 sm:px-8">
        <div class="flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p class="text-xs uppercase tracking-[0.34em] text-cyan-200/80">Welcome Back</p>
            <h2 class="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
              {{ authStore.profile?.username }}
            </h2>
            <p class="mt-4 max-w-2xl text-base leading-7 text-slate-300">
              {{ authStore.profile?.email }} · {{ authStore.profile?.city ?? 'Город не указан' }}
            </p>
          </div>

          <div class="grid gap-3 sm:grid-cols-2">
            <RouterLink class="btn-primary" to="/calls">
              Открыть сценарий звонка
            </RouterLink>
            <RouterLink v-if="authStore.profile?.role === 'Admin'" class="btn-secondary" to="/admin">
              Перейти в админку
            </RouterLink>
          </div>
        </div>

        <div class="mt-8 grid gap-4 md:grid-cols-3">
          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Роль</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ formatRole(authStore.profile?.role ?? 'Client') }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              Разграничение доступа выполняется до любых действий пользователя.
            </p>
          </article>

          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Линия</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ authStore.profile?.phoneNumber ?? 'Не привязана' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              Источник звонка определяется сервером по закреплённому абоненту.
            </p>
          </article>

          <article class="metric-card rounded-[1.5rem] p-5">
            <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Активный звонок</p>
            <h3 class="mt-4 text-2xl font-semibold text-white">
              {{ callStore.activeCall?.destPhone ?? 'Нет активного вызова' }}
            </h3>
            <p class="mt-2 text-sm leading-6 text-slate-300">
              {{ callStore.activeCall ? `Стартовал ${formatDateTime(callStore.activeCall.startedAtUtc)}` : 'Можно безопасно запускать новый звонок.' }}
            </p>
          </article>
        </div>
      </section>

      <section class="panel-surface rounded-[2rem] px-6 py-6 sm:px-8">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-xs uppercase tracking-[0.3em] text-cyan-200/80">Период отчёта</p>
            <h3 class="mt-3 text-2xl font-semibold text-white">Сводка начислений</h3>
          </div>
          <button class="btn-secondary" type="button" :disabled="isLoading" @click="loadInsights">
            {{ isLoading ? 'Обновляем...' : 'Обновить' }}
          </button>
        </div>

        <div class="mt-6 grid gap-4">
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">С</span>
            <input v-model="period.from" class="input-shell" type="date" />
          </label>

          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">По</span>
            <input v-model="period.to" class="input-shell" type="date" />
          </label>
        </div>

        <div class="mt-6 grid gap-4 sm:grid-cols-2">
          <article class="metric-card rounded-[1.4rem] p-4">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Всего звонков</p>
            <p class="mt-3 text-3xl font-semibold text-white">{{ summary?.totalCalls ?? 0 }}</p>
          </article>

          <article class="metric-card rounded-[1.4rem] p-4">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Минуты</p>
            <p class="mt-3 text-3xl font-semibold text-white">{{ summary?.totalMinutes ?? 0 }}</p>
          </article>

          <article class="metric-card rounded-[1.4rem] p-4">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">Базовая стоимость</p>
            <p class="mt-3 text-3xl font-semibold text-white">{{ formatCurrency(summary?.baseCost ?? 0) }}</p>
          </article>

          <article class="metric-card rounded-[1.4rem] p-4">
            <p class="text-xs uppercase tracking-[0.24em] text-slate-300/80">К оплате</p>
            <p class="mt-3 text-3xl font-semibold text-white">{{ formatCurrency(summary?.totalCost ?? 0) }}</p>
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

    <section class="panel-surface mt-6 rounded-[2rem] px-6 py-6 sm:px-8">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p class="text-xs uppercase tracking-[0.3em] text-cyan-200/80">Client History</p>
          <h3 class="mt-3 text-2xl font-semibold text-white">Последние звонки</h3>
          <p class="mt-2 text-sm leading-6 text-slate-300">
            Клиент видит только собственную историю и итоговые суммы за выбранный период.
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
              <td class="font-medium text-white">{{ item.destPhone }}</td>
              <td>{{ item.destinationCity }}</td>
              <td>{{ formatDateTime(item.startedAtUtc) }}</td>
              <td>{{ formatTimeOfDay(item.timeOfDay) }} · {{ formatCurrency(item.appliedTariff) }}</td>
              <td>{{ item.durationMinutes }}</td>
              <td>{{ item.discountPercent.toFixed(2) }}%</td>
              <td class="font-medium text-white">{{ formatCurrency(item.finalCost) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else class="mt-6 rounded-[1.6rem] border border-dashed border-white/12 bg-white/3 px-5 py-10 text-center">
        <p class="text-lg font-semibold text-white">История пока пуста</p>
        <p class="mt-3 text-sm leading-6 text-slate-300">
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
