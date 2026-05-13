<template>
  <AuthShell
    title="Создайте аккаунт абонента"
    lead="Заполните контактные данные один раз, чтобы видеть историю звонков, начисления и актуальные сведения по своему номеру."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs">
          <UiIcon name="user" :size="15" />
          Новый аккаунт
        </span>
        <div>
          <h2 class="text-3xl font-black tracking-tight text-slate-950 sm:text-[2.2rem]">
            Создать аккаунт
          </h2>
          <p class="mt-3 max-w-xl text-sm leading-6 text-slate-600">
            После регистрации подтвердите email кодом из письма. Это займёт меньше минуты.
          </p>
        </div>
      </div>

      <MessageBanner
        v-if="message"
        :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
        :message="message"
        :tone="messageTone"
      />

      <div class="grid gap-4 md:grid-cols-2">
        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">Имя пользователя</span>
          <input v-model.trim="form.username" class="input-shell" required maxlength="15" minlength="5" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">Email</span>
          <input v-model.trim="form.email" class="input-shell" type="email" required maxlength="100" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">Пароль</span>
          <input v-model="form.password" class="input-shell" type="password" required minlength="12" maxlength="100" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">Телефон</span>
          <input v-model.trim="form.phoneNumber" class="input-shell" placeholder="+7123456789" required maxlength="20" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">ИНН</span>
          <input v-model.trim="form.inn" class="input-shell" required maxlength="12" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-bold text-slate-700">Город</span>
          <select v-model.number="form.cityId" class="select-shell" required>
            <option disabled value="0">Выберите город</option>
            <option v-for="city in cities" :key="city.id" :value="city.id">
              {{ city.name }}
            </option>
          </select>
        </label>
      </div>

      <div class="rounded-[1.25rem] border border-blue-100 bg-blue-50 p-4 text-sm leading-6 text-slate-700">
        Пароль должен быть длиной не менее 12 символов и содержать строчные и прописные буквы, цифры и специальный символ.
      </div>

      <label class="block space-y-2">
        <span class="text-sm font-bold text-slate-700">Адрес</span>
        <textarea v-model.trim="form.address" class="textarea-shell min-h-28" rows="3" required maxlength="250" />
      </label>

      <button class="btn-primary w-full" :disabled="isSubmitting || isLoadingCities">
        <UiIcon name="plus" :size="18" />
        {{ isSubmitting ? 'Создаём аккаунт...' : 'Создать аккаунт' }}
      </button>

      <div class="flex flex-col gap-3 text-sm text-slate-600 sm:flex-row sm:items-center sm:justify-between">
        <span>Уже есть аккаунт?</span>
        <RouterLink class="font-bold text-blue-700 transition hover:text-blue-900" to="/login">
          Войти
        </RouterLink>
      </div>
    </form>
  </AuthShell>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AuthShell from '@/components/AuthShell.vue'
import MessageBanner from '@/components/MessageBanner.vue'
import UiIcon from '@/components/UiIcon.vue'
import { ApiError, fetchCities, type CityOption } from '@/lib/api'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()

const cities = ref<CityOption[]>([])
const isLoadingCities = ref(true)
const isSubmitting = ref(false)
const message = ref('')
const messageTone = ref<'success' | 'danger'>('success')

const form = reactive({
  username: '',
  email: '',
  password: '',
  phoneNumber: '',
  inn: '',
  address: '',
  cityId: 0
})

onMounted(async () => {
  try {
    cities.value = await fetchCities()
    if (cities.value.length > 0) {
      form.cityId = cities.value[0].id
    }
  } catch {
    message.value = 'Не удалось загрузить список городов.'
    messageTone.value = 'danger'
  } finally {
    isLoadingCities.value = false
  }
})

async function submit() {
  isSubmitting.value = true
  message.value = ''

  try {
    const response = await authStore.signUp(form)
    message.value = response.message
    messageTone.value = response.code === 'registered_delivery_failed' ? 'danger' : 'success'
    await router.push('/confirm-email')
  } catch (cause) {
    message.value = cause instanceof ApiError ? cause.message : 'Не удалось завершить регистрацию.'
    messageTone.value = 'danger'
  } finally {
    isSubmitting.value = false
  }
}
</script>
