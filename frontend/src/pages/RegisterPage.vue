<template>
  <AuthShell
    title="Регистрация клиента"
    lead="Регистрация сразу создаёт учётную запись и профиль абонента: номер, ИНН, адрес и город сохраняются для последующего учёта звонков и расчёта начислений."
  >
    <form class="space-y-6" @submit.prevent="submit">
      <div class="space-y-3">
        <span class="badge-shell text-xs uppercase tracking-[0.22em] text-cyan-100">
          Registration Flow
        </span>
        <div>
          <h2 class="text-3xl font-semibold tracking-tight text-white sm:text-[2.2rem]">
            Создать аккаунт
          </h2>
          <p class="mt-3 max-w-xl text-sm leading-6 text-slate-300">
            После регистрации система выдаст короткую verification-cookie-сессию и попросит подтвердить email кодом из письма.
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
          <span class="text-sm font-medium text-slate-300">Имя пользователя</span>
          <input v-model.trim="form.username" class="input-shell" required maxlength="15" minlength="5" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-medium text-slate-300">Email</span>
          <input v-model.trim="form.email" class="input-shell" type="email" required maxlength="100" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-medium text-slate-300">Пароль</span>
          <input v-model="form.password" class="input-shell" type="password" required minlength="12" maxlength="100" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-medium text-slate-300">Телефон</span>
          <input v-model.trim="form.phoneNumber" class="input-shell" placeholder="+48123456789" required maxlength="20" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-medium text-slate-300">ИНН</span>
          <input v-model.trim="form.inn" class="input-shell" required maxlength="12" />
        </label>

        <label class="block space-y-2">
          <span class="text-sm font-medium text-slate-300">Город</span>
          <select v-model.number="form.cityId" class="select-shell" required>
            <option disabled value="0">Выберите город</option>
            <option v-for="city in cities" :key="city.id" :value="city.id">
              {{ city.name }}
            </option>
          </select>
        </label>
      </div>

      <div class="rounded-[1.4rem] border border-white/10 bg-white/4 p-4 text-sm leading-6 text-slate-300">
        Пароль должен быть длиной не менее 12 символов и содержать строчные и прописные буквы, цифры и специальный символ.
      </div>

      <label class="block space-y-2">
        <span class="text-sm font-medium text-slate-300">Адрес</span>
        <textarea v-model.trim="form.address" class="textarea-shell min-h-28" rows="3" required maxlength="250" />
      </label>

      <button class="btn-primary w-full" :disabled="isSubmitting || isLoadingCities">
        {{ isSubmitting ? 'Создаём защищённую учётную запись...' : 'Создать аккаунт' }}
      </button>

      <div class="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
        <span>Уже есть аккаунт?</span>
        <RouterLink class="font-semibold text-cyan-200 transition hover:text-white" to="/login">
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
