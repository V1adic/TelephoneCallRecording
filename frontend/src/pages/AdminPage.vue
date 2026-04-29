<template>
  <AppShell
    title="Администрирование"
    description="Управляйте пользователями, городами, тарифами, скидками и отчётами в одном рабочем пространстве."
  >
    <div class="panel-surface animate-rise rounded-[1.75rem] px-5 py-5 sm:px-6">
      <div class="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div>
          <p class="text-sm font-bold text-blue-700">Рабочая область администратора</p>
          <h2 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Справочники и отчёты</h2>
        </div>

        <div class="flex flex-wrap gap-2">
          <button
            v-for="item in tabs"
            :key="item.id"
            type="button"
            :class="['nav-link', tab === item.id ? 'is-active' : '']"
            @click="tab = item.id"
          >
            <UiIcon :name="item.icon" :size="18" />
            {{ item.label }}
          </button>
        </div>
      </div>
    </div>

    <MessageBanner
      v-if="message"
      class="mt-6"
      :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
      :message="message"
      :tone="messageTone"
    />

    <section v-if="tab === 'accounts'" class="mt-6 space-y-6">
      <div class="panel-strong rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex items-start gap-4">
          <div class="icon-box">
            <UiIcon name="users" :size="22" />
          </div>
          <div>
            <p class="text-sm font-bold text-blue-700">Пользователи</p>
            <h3 class="mt-2 text-3xl font-black text-slate-950">Учётные записи и профили</h3>
            <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
              Меняйте роли, восстанавливайте доступ и обновляйте данные абонента: телефон, ИНН, адрес и город.
            </p>
          </div>
        </div>
      </div>

      <div class="grid gap-4 xl:grid-cols-2">
        <article
          v-for="user in users"
          :key="user.id"
          class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6"
        >
          <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <div class="flex flex-wrap gap-2">
                <span class="badge-shell text-xs">{{ formatRole(user.role) }}</span>
                <span :class="accessStateClass(user.accessState)" class="badge-shell text-xs">
                  {{ formatAccessState(user.accessState) }}
                </span>
                <span
                  :class="user.isEmailConfirmed ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-amber-200 bg-amber-50 text-amber-700'"
                  class="badge-shell text-xs"
                >
                  {{ user.isEmailConfirmed ? 'Email подтверждён' : 'Email ожидает подтверждения' }}
                </span>
              </div>

              <h4 class="mt-4 text-2xl font-black text-slate-950">
                {{ user.username }}
              </h4>
              <p class="mt-2 text-sm leading-6 text-slate-600">{{ user.email }}</p>
            </div>

            <div class="flex flex-wrap gap-2">
              <button class="btn-secondary text-sm" type="button" @click="saveRole(user)">
                <UiIcon name="save" :size="16" />
                Роль
              </button>
              <button
                v-if="user.accessState === 'revoked'"
                class="btn-primary text-sm"
                type="button"
                @click="restoreAccess(user)"
              >
                <UiIcon name="check" :size="16" />
                Восстановить
              </button>
              <button
                v-else
                class="btn-danger text-sm"
                type="button"
                :disabled="user.role === 'Admin'"
                @click="revokeAccess(user)"
              >
                <UiIcon name="lock" :size="16" />
                Отозвать
              </button>
            </div>
          </div>

          <div class="mt-6 grid gap-4 md:grid-cols-2">
            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Роль</span>
              <select v-model="user.role" class="select-shell">
                <option value="Client">Клиент</option>
                <option value="Admin">Администратор</option>
              </select>
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Город абонента</span>
              <select v-model.number="user.cityId" class="select-shell" :disabled="!user.subscriberId">
                <option disabled :value="null">Выберите город</option>
                <option v-for="city in cities" :key="city.id" :value="city.id">
                  {{ city.name }}
                </option>
              </select>
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Телефон</span>
              <input v-model="user.phone" class="input-shell" :disabled="!user.subscriberId" maxlength="20" />
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">ИНН</span>
              <input v-model="user.inn" class="input-shell" :disabled="!user.subscriberId" maxlength="12" />
            </label>
          </div>

          <label class="mt-4 block space-y-2">
            <span class="text-sm font-bold text-slate-700">Адрес</span>
            <textarea
              v-model="user.address"
              class="textarea-shell min-h-28"
              :disabled="!user.subscriberId"
            />
          </label>

          <div class="mt-5 flex flex-wrap items-center gap-3">
            <button class="btn-primary text-sm" type="button" :disabled="!user.subscriberId" @click="saveSubscriber(user)">
              <UiIcon name="save" :size="16" />
              Сохранить профиль
            </button>
            <span v-if="user.lockoutEndUtc" class="text-sm font-medium text-amber-700">
              Блокировка до {{ formatDateTime(user.lockoutEndUtc) }}
            </span>
          </div>
        </article>
      </div>
    </section>

    <section v-else-if="tab === 'cities'" class="mt-6 space-y-6">
      <div class="panel-strong rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex items-start gap-4">
          <div class="icon-box green">
            <UiIcon name="city" :size="22" />
          </div>
          <div>
            <p class="text-sm font-bold text-blue-700">Тарифы</p>
            <h3 class="mt-2 text-3xl font-black text-slate-950">Города и стоимость звонков</h3>
            <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
              Для каждого города задаются дневной и ночной тарифы. Новые звонки будут считаться по актуальным значениям.
            </p>
          </div>
        </div>
      </div>

      <article class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 lg:grid-cols-[1.2fr_0.7fr_0.7fr_auto] lg:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Название города</span>
            <input v-model.trim="newCity.name" class="input-shell" maxlength="100" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Дневной тариф</span>
            <input v-model.number="newCity.dayTariff" class="input-shell" type="number" min="0.01" step="0.01" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Ночной тариф</span>
            <input v-model.number="newCity.nightTariff" class="input-shell" type="number" min="0.01" step="0.01" />
          </label>
          <button class="btn-primary lg:mb-0.5" type="button" @click="addCity">
            <UiIcon name="plus" :size="18" />
            Добавить
          </button>
        </div>
      </article>

      <div class="grid gap-4 xl:grid-cols-2">
        <article v-for="city in cities" :key="city.id" class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h4 class="text-2xl font-black text-slate-950">{{ city.name }}</h4>
              <p class="mt-2 text-sm leading-6 text-slate-600">
                Абонентов: {{ city.subscribersCount }} · звонков в истории: {{ city.callsCount }}
              </p>
            </div>

            <button class="btn-danger text-sm" type="button" @click="removeCity(city)">
              <UiIcon name="trash" :size="16" />
              Удалить
            </button>
          </div>

          <div class="mt-6 grid gap-4 md:grid-cols-3">
            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Название</span>
              <input v-model.trim="city.name" class="input-shell" maxlength="100" />
            </label>
            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Дневной тариф</span>
              <input v-model.number="city.dayTariff" class="input-shell" type="number" min="0.01" step="0.01" />
            </label>
            <label class="block space-y-2">
              <span class="text-sm font-bold text-slate-700">Ночной тариф</span>
              <input v-model.number="city.nightTariff" class="input-shell" type="number" min="0.01" step="0.01" />
            </label>
          </div>

          <div class="mt-5 flex justify-end">
            <button class="btn-primary text-sm" type="button" @click="saveCity(city)">
              <UiIcon name="save" :size="16" />
              Сохранить изменения
            </button>
          </div>
        </article>
      </div>
    </section>

    <section v-else-if="tab === 'discounts'" class="mt-6 space-y-6">
      <div class="panel-strong rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex items-start gap-4">
          <div class="icon-box amber">
            <UiIcon name="percent" :size="22" />
          </div>
          <div>
            <p class="text-sm font-bold text-blue-700">Скидки</p>
            <h3 class="mt-2 text-3xl font-black text-slate-950">Правила по длительности</h3>
            <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
              Задавайте скидки для диапазонов минут. Правила должны быть понятными и не пересекаться между собой.
            </p>
          </div>
        </div>
      </div>

      <article class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 lg:grid-cols-[1fr_0.6fr_0.6fr_0.7fr_auto] lg:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Город</span>
            <select v-model.number="newDiscount.cityId" class="select-shell">
              <option disabled :value="0">Выберите город</option>
              <option v-for="city in cities" :key="city.id" :value="city.id">
                {{ city.name }}
              </option>
            </select>
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Минут от</span>
            <input v-model.number="newDiscount.minMinutes" class="input-shell" type="number" min="1" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Минут до</span>
            <input v-model="newDiscount.maxMinutes" class="input-shell" type="number" min="1" placeholder="∞" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Скидка, %</span>
            <input v-model.number="newDiscount.discountPercent" class="input-shell" type="number" min="0" max="100" step="0.01" />
          </label>
          <button class="btn-primary lg:mb-0.5" type="button" @click="addDiscount">
            <UiIcon name="plus" :size="18" />
            Добавить
          </button>
        </div>
      </article>

      <div class="grid gap-4 xl:grid-cols-2">
        <article v-for="city in cities" :key="city.id" class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h4 class="text-2xl font-black text-slate-950">{{ city.name }}</h4>
              <p class="mt-2 text-sm leading-6 text-slate-600">
                {{ discountsFor(city.id).length ? 'Активные правила для длительных звонков.' : 'Для этого города скидки ещё не заданы.' }}
              </p>
            </div>

            <span class="badge-shell text-xs">
              {{ discountsFor(city.id).length }} правил
            </span>
          </div>

          <div v-if="discountsFor(city.id).length" class="mt-5 space-y-3">
            <article
              v-for="discount in discountsFor(city.id)"
              :key="discount.id"
              class="rounded-[1.25rem] border border-slate-200 bg-slate-50 px-4 py-4"
            >
              <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p class="text-sm font-black text-slate-950">
                    от {{ discount.minMinutes }} мин
                    <span v-if="discount.maxMinutes !== null">до {{ discount.maxMinutes }} мин</span>
                    <span v-else>и выше</span>
                  </p>
                  <p class="mt-2 text-sm leading-6 text-slate-600">
                    Скидка {{ discount.discountPercent.toFixed(2) }}%
                  </p>
                </div>

                <button class="btn-danger text-sm" type="button" @click="removeDiscount(discount.id)">
                  <UiIcon name="trash" :size="16" />
                  Удалить
                </button>
              </div>
            </article>
          </div>
        </article>
      </div>
    </section>

    <section v-else class="mt-6 space-y-6">
      <div class="panel-strong rounded-[1.75rem] px-6 py-6 sm:px-8">
        <div class="flex items-start gap-4">
          <div class="icon-box">
            <UiIcon name="fileChart" :size="22" />
          </div>
          <div>
            <p class="text-sm font-bold text-blue-700">Отчёты</p>
            <h3 class="mt-2 text-3xl font-black text-slate-950">Начисления и детализация</h3>
            <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
              Выберите период, чтобы увидеть подробный журнал звонков, суммы по городам и сводку по конкретному номеру.
            </p>
          </div>
        </div>
      </div>

      <article class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 xl:grid-cols-[0.7fr_0.7fr_1fr_auto] xl:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">С</span>
            <input v-model="reportPeriod.from" class="input-shell" type="date" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">По</span>
            <input v-model="reportPeriod.to" class="input-shell" type="date" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-bold text-slate-700">Номер абонента для сводки</span>
            <input v-model.trim="subscriberPhone" class="input-shell" placeholder="+48123456789" />
          </label>
          <button class="btn-primary xl:mb-0.5" type="button" :disabled="reportsLoading" @click="loadReports">
            <UiIcon name="barChart" :size="18" />
            {{ reportsLoading ? 'Формируем...' : 'Построить отчёт' }}
          </button>
        </div>
      </article>

      <div class="grid gap-4 lg:grid-cols-3">
        <article class="metric-card p-5">
          <div class="icon-box">
            <UiIcon name="history" :size="20" />
          </div>
          <p class="mt-4 text-sm font-bold text-slate-500">Детализация</p>
          <p class="mt-2 text-3xl font-black text-slate-950">{{ callReport.length }}</p>
          <p class="mt-2 text-sm leading-6 text-slate-600">Записей в журнале за выбранный период.</p>
        </article>

        <article class="metric-card p-5">
          <div class="icon-box green">
            <UiIcon name="city" :size="20" />
          </div>
          <p class="mt-4 text-sm font-bold text-slate-500">Города</p>
          <p class="mt-2 text-3xl font-black text-slate-950">{{ cityReport.length }}</p>
          <p class="mt-2 text-sm leading-6 text-slate-600">Строк по направлениям вызовов.</p>
        </article>

        <article class="metric-card p-5">
          <div class="icon-box amber">
            <UiIcon name="wallet" :size="20" />
          </div>
          <p class="mt-4 text-sm font-bold text-slate-500">Абонент</p>
          <p class="mt-2 text-3xl font-black text-slate-950">
            {{ subscriberReport.length ? formatCurrency(subscriberReport[0].totalCost) : '—' }}
          </p>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            {{ subscriberReport.length ? `Сумма по номеру ${subscriberReport[0].phoneNumber}` : 'Сводка появится после указания номера.' }}
          </p>
        </article>
      </div>

      <section class="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
        <article class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
          <h4 class="text-2xl font-black text-slate-950">По городам</h4>
          <div v-if="cityReport.length" class="mt-5 space-y-3">
            <article
              v-for="item in cityReport"
              :key="item.name"
              class="rounded-[1.25rem] border border-slate-200 bg-slate-50 px-4 py-4"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-lg font-black text-slate-950">{{ item.name }}</p>
                  <p class="mt-2 text-sm leading-6 text-slate-600">
                    {{ item.totalCalls }} звонков · {{ item.totalMinutes }} минут
                  </p>
                </div>
                <div class="text-right text-slate-950">
                  <p class="text-sm text-slate-500">Итог</p>
                  <p class="mt-2 text-xl font-black">{{ formatCurrency(item.totalCost) }}</p>
                </div>
              </div>
            </article>
          </div>
          <div v-else class="mt-5 rounded-[1.25rem] border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-sm text-slate-600">
            После запуска отчёта здесь появятся начисления по городам.
          </div>
        </article>

        <article class="panel-surface rounded-[1.5rem] px-5 py-5 sm:px-6">
          <h4 class="text-2xl font-black text-slate-950">Подробная детализация</h4>
          <div v-if="callReport.length" class="table-shell soft-scroll mt-5 overflow-x-auto">
            <table class="data-table min-w-[1080px]">
              <thead>
                <tr>
                  <th>Источник</th>
                  <th>Получатель</th>
                  <th>Город</th>
                  <th>Старт</th>
                  <th>Минуты</th>
                  <th>Тариф</th>
                  <th>Скидка</th>
                  <th>Итог</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in callReport" :key="item.callId">
                  <td class="font-bold text-slate-950">{{ item.sourcePhone }}</td>
                  <td>{{ item.destPhone }}</td>
                  <td>{{ item.destinationCity }}</td>
                  <td>{{ formatDateTime(item.startedAtUtc) }}</td>
                  <td>{{ item.durationMinutes }}</td>
                  <td>{{ formatTimeOfDay(item.timeOfDay) }} · {{ formatCurrency(item.appliedTariff) }}</td>
                  <td>{{ item.discountPercent.toFixed(2) }}%</td>
                  <td class="font-bold text-slate-950">{{ formatCurrency(item.finalCost) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="mt-5 rounded-[1.25rem] border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-sm text-slate-600">
            Детализация звонков появится после построения отчёта за выбранный период.
          </div>
        </article>
      </section>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import AppShell from '@/components/AppShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import UiIcon from '@/components/UiIcon.vue'
import {
  ApiError,
  changeUserRole,
  createCity,
  createDiscount,
  deleteCity as deleteCityRequest,
  deleteDiscount as deleteDiscountRequest,
  fetchAdminCities,
  fetchAdminUsers,
  fetchCallDetailsReport,
  fetchCityCostsReport,
  fetchSubscriberCostsReport,
  getDiscounts,
  restoreUserAccess,
  revokeUserAccess,
  updateCity as updateCityRequest,
  updateSubscriber,
  type AdminCity,
  type AdminUser,
  type CallDetailReport,
  type CityCostReport,
  type CityDiscount,
  type SubscriberCostReport
} from '@/lib/api'
import {
  createDefaultPeriod,
  formatAccessState,
  formatCurrency,
  formatDateTime,
  formatRole,
  formatTimeOfDay,
  periodToUtc
} from '@/lib/format'

const tabs = [
  { id: 'accounts', label: 'Аккаунты', icon: 'users' },
  { id: 'cities', label: 'Города', icon: 'city' },
  { id: 'discounts', label: 'Скидки', icon: 'percent' },
  { id: 'reports', label: 'Отчёты', icon: 'fileChart' }
] as const

type AdminTab = (typeof tabs)[number]['id']

const tab = ref<AdminTab>('accounts')
const users = ref<AdminUser[]>([])
const cities = ref<AdminCity[]>([])
const discounts = ref<Record<number, CityDiscount[]>>({})

const newCity = reactive({
  name: '',
  dayTariff: 1,
  nightTariff: 1
})

const newDiscount = reactive({
  cityId: 0,
  minMinutes: 1,
  maxMinutes: '',
  discountPercent: 0
})

const reportPeriod = ref(createDefaultPeriod())
const subscriberPhone = ref('')
const callReport = ref<CallDetailReport[]>([])
const cityReport = ref<CityCostReport[]>([])
const subscriberReport = ref<SubscriberCostReport[]>([])
const reportsLoading = ref(false)

const message = ref('')
const messageTone = ref<'success' | 'danger'>('success')

function accessStateClass(state: string) {
  const classes = {
    active: 'border-emerald-200 bg-emerald-50 text-emerald-700',
    locked: 'border-amber-200 bg-amber-50 text-amber-700',
    revoked: 'border-rose-200 bg-rose-50 text-rose-700'
  }

  return classes[state as keyof typeof classes] ?? classes.active
}

function discountsFor(cityId: number) {
  return discounts.value[cityId] ?? []
}

function showMessage(text: string, tone: 'success' | 'danger') {
  message.value = text
  messageTone.value = tone
}

async function loadAdminData() {
  try {
    const [nextUsers, nextCities] = await Promise.all([
      fetchAdminUsers(),
      fetchAdminCities()
    ])

    users.value = nextUsers
    cities.value = nextCities

    const discountPairs = await Promise.all(
      nextCities.map(async (city) => [city.id, await getDiscounts(city.id)] as const)
    )

    discounts.value = Object.fromEntries(discountPairs)

    if (!newDiscount.cityId && nextCities.length) {
      newDiscount.cityId = nextCities[0].id
    }
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось загрузить административные данные.', 'danger')
  }
}

async function saveRole(user: AdminUser) {
  try {
    const response = await changeUserRole(user.id, user.role)
    showMessage(response.message, 'success')
    users.value = await fetchAdminUsers()
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось изменить роль.', 'danger')
    users.value = await fetchAdminUsers()
  }
}

async function saveSubscriber(user: AdminUser) {
  if (!user.subscriberId || !user.phone || !user.inn || !user.address || !user.cityId) {
    showMessage('Для сохранения профиля абонента заполните телефон, ИНН, адрес и город.', 'danger')
    return
  }

  try {
    const updated = await updateSubscriber(user.id, {
      phone: user.phone,
      inn: user.inn,
      address: user.address,
      cityId: user.cityId
    })

    users.value = users.value.map((item) => (item.id === updated.id ? updated : item))
    showMessage('Профиль абонента обновлён.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось обновить профиль абонента.', 'danger')
    users.value = await fetchAdminUsers()
  }
}

async function revokeAccess(user: AdminUser) {
  try {
    const response = await revokeUserAccess(user.id)
    showMessage(response.message, 'success')
    users.value = await fetchAdminUsers()
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось отозвать доступ.', 'danger')
  }
}

async function restoreAccess(user: AdminUser) {
  try {
    const response = await restoreUserAccess(user.id)
    showMessage(response.message, 'success')
    users.value = await fetchAdminUsers()
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось восстановить доступ.', 'danger')
  }
}

async function addCity() {
  try {
    const city = await createCity(newCity.name, newCity.dayTariff, newCity.nightTariff)
    cities.value = [...cities.value, city].sort((a, b) => a.name.localeCompare(b.name, 'ru'))
    discounts.value[city.id] = []
    newCity.name = ''
    newCity.dayTariff = 1
    newCity.nightTariff = 1
    showMessage('Город добавлен.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось добавить город.', 'danger')
  }
}

async function saveCity(city: AdminCity) {
  try {
    const updated = await updateCityRequest(city.id, city.name, city.dayTariff, city.nightTariff)
    cities.value = cities.value
      .map((item) => (item.id === updated.id ? updated : item))
      .sort((a, b) => a.name.localeCompare(b.name, 'ru'))
    showMessage('Тарифы города обновлены.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось обновить город.', 'danger')
    cities.value = await fetchAdminCities()
  }
}

async function removeCity(city: AdminCity) {
  try {
    await deleteCityRequest(city.id)
    cities.value = cities.value.filter((item) => item.id !== city.id)
    delete discounts.value[city.id]
    showMessage('Город удалён.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось удалить город.', 'danger')
  }
}

async function addDiscount() {
  try {
    const maxMinutes = newDiscount.maxMinutes === '' ? null : Number(newDiscount.maxMinutes)
    const created = await createDiscount(
      newDiscount.cityId,
      newDiscount.minMinutes,
      maxMinutes,
      newDiscount.discountPercent
    )

    discounts.value = {
      ...discounts.value,
      [created.cityId]: [...discountsFor(created.cityId), created].sort((a, b) => a.minMinutes - b.minMinutes)
    }

    newDiscount.minMinutes = 1
    newDiscount.maxMinutes = ''
    newDiscount.discountPercent = 0
    showMessage('Скидка добавлена.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось добавить скидку.', 'danger')
  }
}

async function removeDiscount(id: number) {
  try {
    await deleteDiscountRequest(id)
    discounts.value = Object.fromEntries(
      Object.entries(discounts.value).map(([cityId, items]) => [Number(cityId), items.filter((item) => item.id !== id)])
    )
    showMessage('Скидка удалена.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось удалить скидку.', 'danger')
  }
}

async function loadReports() {
  reportsLoading.value = true

  try {
    const { fromUtc, toUtc } = periodToUtc(reportPeriod.value)
    const [details, byCities, bySubscriber] = await Promise.all([
      fetchCallDetailsReport(fromUtc, toUtc),
      fetchCityCostsReport(fromUtc, toUtc),
      subscriberPhone.value
        ? fetchSubscriberCostsReport(subscriberPhone.value, fromUtc, toUtc)
        : Promise.resolve([])
    ])

    callReport.value = details
    cityReport.value = byCities
    subscriberReport.value = bySubscriber
    showMessage('Отчёты сформированы.', 'success')
  } catch (error) {
    showMessage(error instanceof ApiError ? error.message : 'Не удалось сформировать отчёты.', 'danger')
  } finally {
    reportsLoading.value = false
  }
}

onMounted(async () => {
  await loadAdminData()
})
</script>
