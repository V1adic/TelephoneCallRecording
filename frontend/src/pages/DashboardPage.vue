<template>
  <AppShell title="Личный кабинет" @logout="logout">
    <section class="workspace-grid">
      <article class="workspace-lead">
        <p class="eyebrow">Профиль</p>
        <h2>{{ authStore.profile?.username }}</h2>
        <p class="workspace-lead__copy">
          {{ authStore.profile?.email }} · {{ authStore.profile?.city ?? 'Город не указан' }}
        </p>
      </article>

      <article class="workspace-panel">
        <p class="eyebrow">Абонент</p>
        <h3>{{ authStore.profile?.phoneNumber ?? 'Телефон не привязан' }}</h3>
        <p>В v1 один пользователь управляет одним абонентским номером.</p>
      </article>

      <article class="workspace-panel">
        <p class="eyebrow">Активный звонок</p>
        <template v-if="callStore.activeCall">
          <h3>{{ callStore.activeCall.destPhone }}</h3>
          <p>
            Начат {{ formatDate(callStore.activeCall.startedAtUtc) }} · {{ callStore.activeCall.timeOfDay }}
          </p>
          <RouterLink class="text-link" to="/calls">Перейти к управлению звонком</RouterLink>
        </template>
        <template v-else>
          <h3>Нет активного звонка</h3>
          <p>Можно сразу открыть рабочий экран и запустить новый вызов.</p>
          <RouterLink class="text-link" to="/calls">Открыть экран звонков</RouterLink>
        </template>
      </article>

      <article class="workspace-panel">
        <p class="eyebrow">Последний расчёт</p>
        <template v-if="callStore.lastCompletedCall">
          <h3>{{ callStore.lastCompletedCall.cost.toFixed(2) }} ₽</h3>
          <p>
            Скидка {{ callStore.lastCompletedCall.discountPercent.toFixed(2) }}% ·
            без скидки {{ callStore.lastCompletedCall.baseCost.toFixed(2) }} ₽
          </p>
        </template>
        <template v-else>
          <h3>Пока нет завершённых звонков</h3>
          <p>После завершения вызова итоговая стоимость появится здесь.</p>
        </template>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import { useAuthStore } from '@/stores/auth'
import { useCallStore } from '@/stores/call'

const authStore = useAuthStore()
const callStore = useCallStore()
const router = useRouter()

onMounted(async () => {
  await callStore.loadActiveCall()
})

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
