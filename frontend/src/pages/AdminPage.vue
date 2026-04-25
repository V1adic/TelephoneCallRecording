<template>
  <AppShell
    title="Административный контур"
    description="Администратор управляет учётными записями, абонентскими профилями, тарифами, скидками и отчётами. Все критичные операции выполняются через защищённый cookie-based API с CSRF-контролем."
  >
    <div class="panel-surface rounded-[2rem] px-5 py-5 sm:px-6">
      <div class="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div>
          <p class="text-xs uppercase tracking-[0.34em] text-cyan-200/80">Admin Workspace</p>
          <h2 class="mt-3 text-3xl font-semibold tracking-tight text-white">Справочники и отчёты</h2>
        </div>

        <div class="flex flex-wrap gap-2">
          <button
            v-for="item in tabs"
            :key="item.id"
            type="button"
            :class="['nav-link', tab === item.id ? 'is-active' : '']"
            @click="tab = item.id"
          >
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
      <div class="panel-strong rounded-[2rem] px-6 py-6 sm:px-8">
        <p class="text-xs uppercase tracking-[0.32em] text-cyan-200/80">Accounts</p>
        <h3 class="mt-3 text-3xl font-semibold text-white">Учётные записи и профили абонентов</h3>
        <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-300">
          Здесь администратор видит только зарегистрированные аккаунты, может менять роль, отзывать или восстанавливать доступ,
          а также корректировать телефонный профиль, ИНН, адрес и город.
        </p>
      </div>

      <div class="grid gap-4 xl:grid-cols-2">
        <article
          v-for="user in users"
          :key="user.id"
          class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6"
        >
          <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <div class="flex flex-wrap gap-2">
                <span class="badge-shell text-xs">{{ formatRole(user.role) }}</span>
                <span :class="accessStateClass(user.accessState)" class="badge-shell text-xs">
                  {{ formatAccessState(user.accessState) }}
                </span>
                <span
                  :class="user.isEmailConfirmed ? 'border-emerald-300/25 bg-emerald-300/10 text-emerald-100' : 'border-amber-300/25 bg-amber-300/10 text-amber-100'"
                  class="badge-shell text-xs"
                >
                  {{ user.isEmailConfirmed ? 'Email подтверждён' : 'Email не подтверждён' }}
                </span>
              </div>

              <h4 class="mt-4 text-2xl font-semibold text-white">
                {{ user.username }}
              </h4>
              <p class="mt-2 text-sm leading-6 text-slate-300">{{ user.email }}</p>
            </div>

            <div class="flex flex-wrap gap-2">
              <button class="btn-secondary text-sm" type="button" @click="saveRole(user)">
                Сохранить роль
              </button>
              <button
                v-if="user.accessState === 'revoked'"
                class="btn-primary text-sm"
                type="button"
                @click="restoreAccess(user)"
              >
                Восстановить доступ
              </button>
              <button
                v-else
                class="btn-danger text-sm"
                type="button"
                :disabled="user.role === 'Admin'"
                @click="revokeAccess(user)"
              >
                Отозвать доступ
              </button>
            </div>
          </div>

          <div class="mt-6 grid gap-4 md:grid-cols-2">
            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Роль</span>
              <select v-model="user.role" class="select-shell">
                <option value="Client">Client</option>
                <option value="Admin">Admin</option>
              </select>
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Город абонента</span>
              <select v-model.number="user.cityId" class="select-shell" :disabled="!user.subscriberId">
                <option disabled :value="null">Выберите город</option>
                <option v-for="city in cities" :key="city.id" :value="city.id">
                  {{ city.name }}
                </option>
              </select>
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Телефон</span>
              <input v-model="user.phone" class="input-shell" :disabled="!user.subscriberId" maxlength="20" />
            </label>

            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">ИНН</span>
              <input v-model="user.inn" class="input-shell" :disabled="!user.subscriberId" maxlength="12" />
            </label>
          </div>

          <label class="mt-4 block space-y-2">
            <span class="text-sm font-medium text-slate-300">Адрес</span>
            <textarea
              v-model="user.address"
              class="textarea-shell min-h-28"
              :disabled="!user.subscriberId"
            />
          </label>

          <div class="mt-5 flex flex-wrap gap-3">
            <button class="btn-primary text-sm" type="button" :disabled="!user.subscriberId" @click="saveSubscriber(user)">
              Сохранить профиль абонента
            </button>
            <span v-if="user.lockoutEndUtc" class="text-sm text-slate-400">
              Lockout до {{ formatDateTime(user.lockoutEndUtc) }}
            </span>
          </div>
        </article>
      </div>
    </section>

    <section v-else-if="tab === 'cities'" class="mt-6 space-y-6">
      <div class="panel-strong rounded-[2rem] px-6 py-6 sm:px-8">
        <p class="text-xs uppercase tracking-[0.32em] text-cyan-200/80">Cities & Tariffs</p>
        <h3 class="mt-3 text-3xl font-semibold text-white">Города и тарифы</h3>
        <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-300">
          Для каждого города администратор задаёт дневной и ночной тариф. Изменения сразу влияют на расчёт новых звонков и отчётность.
        </p>
      </div>

      <article class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 lg:grid-cols-[1.2fr_0.7fr_0.7fr_auto] lg:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Название города</span>
            <input v-model.trim="newCity.name" class="input-shell" maxlength="100" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Дневной тариф</span>
            <input v-model.number="newCity.dayTariff" class="input-shell" type="number" min="0.01" step="0.01" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Ночной тариф</span>
            <input v-model.number="newCity.nightTariff" class="input-shell" type="number" min="0.01" step="0.01" />
          </label>
          <button class="btn-primary lg:mb-0.5" type="button" @click="addCity">
            Добавить город
          </button>
        </div>
      </article>

      <div class="grid gap-4 xl:grid-cols-2">
        <article v-for="city in cities" :key="city.id" class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h4 class="text-2xl font-semibold text-white">{{ city.name }}</h4>
              <p class="mt-2 text-sm leading-6 text-slate-300">
                Абонентов: {{ city.subscribersCount }} · звонков в истории: {{ city.callsCount }}
              </p>
            </div>

            <button class="btn-danger text-sm" type="button" @click="removeCity(city)">
              Удалить
            </button>
          </div>

          <div class="mt-6 grid gap-4 md:grid-cols-3">
            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Название</span>
              <input v-model.trim="city.name" class="input-shell" maxlength="100" />
            </label>
            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Дневной тариф</span>
              <input v-model.number="city.dayTariff" class="input-shell" type="number" min="0.01" step="0.01" />
            </label>
            <label class="block space-y-2">
              <span class="text-sm font-medium text-slate-300">Ночной тариф</span>
              <input v-model.number="city.nightTariff" class="input-shell" type="number" min="0.01" step="0.01" />
            </label>
          </div>

          <div class="mt-5 flex justify-end">
            <button class="btn-primary text-sm" type="button" @click="saveCity(city)">
              Сохранить изменения
            </button>
          </div>
        </article>
      </div>
    </section>

    <section v-else-if="tab === 'discounts'" class="mt-6 space-y-6">
      <div class="panel-strong rounded-[2rem] px-6 py-6 sm:px-8">
        <p class="text-xs uppercase tracking-[0.32em] text-cyan-200/80">Discount Rules</p>
        <h3 class="mt-3 text-3xl font-semibold text-white">Скидки по длительности</h3>
        <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-300">
          Правила задаются отдельно для каждого города и не должны пересекаться по диапазону минут.
        </p>
      </div>

      <article class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 lg:grid-cols-[1fr_0.6fr_0.6fr_0.7fr_auto] lg:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Город</span>
            <select v-model.number="newDiscount.cityId" class="select-shell">
              <option disabled :value="0">Выберите город</option>
              <option v-for="city in cities" :key="city.id" :value="city.id">
                {{ city.name }}
              </option>
            </select>
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Минут от</span>
            <input v-model.number="newDiscount.minMinutes" class="input-shell" type="number" min="1" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Минут до</span>
            <input v-model="newDiscount.maxMinutes" class="input-shell" type="number" min="1" placeholder="∞" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Скидка, %</span>
            <input v-model.number="newDiscount.discountPercent" class="input-shell" type="number" min="0" max="100" step="0.01" />
          </label>
          <button class="btn-primary lg:mb-0.5" type="button" @click="addDiscount">
            Добавить скидку
          </button>
        </div>
      </article>

      <div class="grid gap-4 xl:grid-cols-2">
        <article v-for="city in cities" :key="city.id" class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h4 class="text-2xl font-semibold text-white">{{ city.name }}</h4>
              <p class="mt-2 text-sm leading-6 text-slate-300">
                {{ discountsFor(city.id).length ? 'Активные правила тарификации по длительности.' : 'Для этого города скидки ещё не заданы.' }}
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
              class="rounded-[1.4rem] border border-white/10 bg-white/4 px-4 py-4"
            >
              <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p class="text-sm font-semibold text-white">
                    от {{ discount.minMinutes }} мин
                    <span v-if="discount.maxMinutes !== null">до {{ discount.maxMinutes }} мин</span>
                    <span v-else>и выше</span>
                  </p>
                  <p class="mt-2 text-sm leading-6 text-slate-300">
                    Скидка {{ discount.discountPercent.toFixed(2) }}%
                  </p>
                </div>

                <button class="btn-danger text-sm" type="button" @click="removeDiscount(discount.id)">
                  Удалить
                </button>
              </div>
            </article>
          </div>
        </article>
      </div>
    </section>

    <section v-else class="mt-6 space-y-6">
      <div class="panel-strong rounded-[2rem] px-6 py-6 sm:px-8">
        <p class="text-xs uppercase tracking-[0.32em] text-cyan-200/80">Reports</p>
        <h3 class="mt-3 text-3xl font-semibold text-white">Отчёты по начислениям</h3>
        <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-300">
          Формирование отчётов покрывает детализацию звонков, суммарные начисления по городам и выборочную сводку по конкретному абоненту.
        </p>
      </div>

      <article class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
        <div class="grid gap-4 xl:grid-cols-[0.7fr_0.7fr_1fr_auto] xl:items-end">
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">С</span>
            <input v-model="reportPeriod.from" class="input-shell" type="date" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">По</span>
            <input v-model="reportPeriod.to" class="input-shell" type="date" />
          </label>
          <label class="block space-y-2">
            <span class="text-sm font-medium text-slate-300">Номер абонента для точечной сводки</span>
            <input v-model.trim="subscriberPhone" class="input-shell" placeholder="+48123456789" />
          </label>
          <button class="btn-primary xl:mb-0.5" type="button" :disabled="reportsLoading" @click="loadReports">
            {{ reportsLoading ? 'Формируем...' : 'Построить отчёт' }}
          </button>
        </div>
      </article>

      <div class="grid gap-4 lg:grid-cols-3">
        <article class="panel-surface rounded-[1.8rem] px-5 py-5">
          <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Детализация</p>
          <p class="mt-3 text-3xl font-semibold text-white">{{ callReport.length }}</p>
          <p class="mt-2 text-sm leading-6 text-slate-300">Записей в подробном журнале выбранного периода.</p>
        </article>

        <article class="panel-surface rounded-[1.8rem] px-5 py-5">
          <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Города</p>
          <p class="mt-3 text-3xl font-semibold text-white">{{ cityReport.length }}</p>
          <p class="mt-2 text-sm leading-6 text-slate-300">Агрегированных строк по направлениям вызовов.</p>
        </article>

        <article class="panel-surface rounded-[1.8rem] px-5 py-5">
          <p class="text-xs uppercase tracking-[0.28em] text-slate-300/80">Абонент</p>
          <p class="mt-3 text-3xl font-semibold text-white">
            {{ subscriberReport.length ? formatCurrency(subscriberReport[0].totalCost) : '—' }}
          </p>
          <p class="mt-2 text-sm leading-6 text-slate-300">
            {{ subscriberReport.length ? `Сумма по номеру ${subscriberReport[0].phoneNumber}` : 'Сводка появится после указания номера.' }}
          </p>
        </article>
      </div>

      <section class="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
        <article class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
          <h4 class="text-2xl font-semibold text-white">По городам</h4>
          <div v-if="cityReport.length" class="mt-5 space-y-3">
            <article
              v-for="item in cityReport"
              :key="item.name"
              class="rounded-[1.4rem] border border-white/10 bg-white/4 px-4 py-4"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-lg font-semibold text-white">{{ item.name }}</p>
                  <p class="mt-2 text-sm leading-6 text-slate-300">
                    {{ item.totalCalls }} звонков · {{ item.totalMinutes }} минут
                  </p>
                </div>
                <div class="text-right text-white">
                  <p class="text-sm text-slate-300">Итог</p>
                  <p class="mt-2 text-xl font-semibold">{{ formatCurrency(item.totalCost) }}</p>
                </div>
              </div>
            </article>
          </div>
          <div v-else class="mt-5 rounded-[1.4rem] border border-dashed border-white/12 bg-white/3 px-4 py-8 text-sm text-slate-300">
            После запуска отчёта здесь появятся агрегированные начисления по городам.
          </div>
        </article>

        <article class="panel-surface rounded-[1.9rem] px-5 py-5 sm:px-6">
          <h4 class="text-2xl font-semibold text-white">Подробная детализация</h4>
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
                  <td class="font-medium text-white">{{ item.sourcePhone }}</td>
                  <td>{{ item.destPhone }}</td>
                  <td>{{ item.destinationCity }}</td>
                  <td>{{ formatDateTime(item.startedAtUtc) }}</td>
                  <td>{{ item.durationMinutes }}</td>
                  <td>{{ formatTimeOfDay(item.timeOfDay) }} · {{ formatCurrency(item.appliedTariff) }}</td>
                  <td>{{ item.discountPercent.toFixed(2) }}%</td>
                  <td class="font-medium text-white">{{ formatCurrency(item.finalCost) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="mt-5 rounded-[1.4rem] border border-dashed border-white/12 bg-white/3 px-4 py-8 text-sm text-slate-300">
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
  { id: 'accounts', label: 'Учётные записи' },
  { id: 'cities', label: 'Города' },
  { id: 'discounts', label: 'Скидки' },
  { id: 'reports', label: 'Отчёты' }
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
    active: 'border-emerald-300/25 bg-emerald-300/10 text-emerald-100',
    locked: 'border-amber-300/25 bg-amber-300/10 text-amber-100',
    revoked: 'border-rose-300/25 bg-rose-300/10 text-rose-100'
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
