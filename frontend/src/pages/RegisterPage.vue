<template>
  <AuthShell
    title="Регистрация абонента"
    lead="Первый релиз связывает один аккаунт с одним абонентом, поэтому регистрация сразу создаёт учётную запись и телефонный профиль в выбранном городе."
  >
    <form class="stack" @submit.prevent="submit">
      <div class="form-copy">
        <h2>Создать аккаунт</h2>
        <p>После регистрации система выдаст verification-cookie и попросит подтвердить email кодом.</p>
      </div>

      <MessageBanner
        v-if="message"
        :label="messageTone === 'danger' ? 'Ошибка' : 'Статус'"
        :message="message"
        :tone="messageTone"
      />

      <div class="field-grid">
        <label class="field">
          <span>Username</span>
          <input v-model.trim="form.username" required maxlength="15" minlength="5" />
        </label>

        <label class="field">
          <span>Email</span>
          <input v-model.trim="form.email" type="email" required maxlength="100" />
        </label>

        <label class="field">
          <span>Password</span>
          <input v-model="form.password" type="password" required minlength="12" maxlength="100" />
        </label>

        <label class="field">
          <span>Phone number</span>
          <input v-model.trim="form.phoneNumber" placeholder="+48123456789" required maxlength="20" />
        </label>

        <label class="field">
          <span>INN</span>
          <input v-model.trim="form.inn" required maxlength="12" />
        </label>

        <label class="field">
          <span>City</span>
          <select v-model.number="form.cityId" required>
            <option disabled value="0">Выберите город</option>
            <option v-for="city in cities" :key="city.id" :value="city.id">
              {{ city.name }}
            </option>
          </select>
        </label>
      </div>

      <label class="field">
        <span>Address</span>
        <textarea v-model.trim="form.address" rows="3" required maxlength="250" />
      </label>

      <button class="primary-button" :disabled="isSubmitting || isLoadingCities">
        {{ isSubmitting ? 'Создаём аккаунт...' : 'Создать аккаунт' }}
      </button>

      <RouterLink class="text-link" to="/login">Уже есть аккаунт? Войти</RouterLink>
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
