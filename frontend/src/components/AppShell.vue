<template>
  <div class="app-shell">
    <header class="app-shell__header">
      <div>
        <p class="eyebrow">TelephoneCallRecording</p>
        <h1>{{ title }}</h1>
      </div>
      <nav class="app-shell__nav">
        <RouterLink to="/dashboard">Кабинет</RouterLink>
        <RouterLink to="/calls">Звонки</RouterLink>
        <button class="ghost-button" @click="$emit('logout')">Выйти</button>
      </nav>
    </header>
    <main class="app-shell__body">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'

defineProps<{
  title: string
}>()

defineEmits<{
  logout: []
}>()
</script>
