<template>
  <div class="auth-shell">
    <section class="auth-shell__poster">
      <div class="auth-shell__glow" />
      <p class="eyebrow">TelephoneCallRecording</p>
      <h1>{{ title }}</h1>
      <p class="auth-shell__lead">{{ lead }}</p>
      <ul class="auth-shell__facts">
        <li>Cookie-auth и CSRF для браузерного контура</li>
        <li>Тарифы, скидки и активные звонки в одном рабочем потоке</li>
        <li>Подтверждение email и понятные сообщения об ошибках</li>
      </ul>
    </section>
    <section class="auth-shell__panel">
      <slot />
    </section>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  lead: string
}>()
</script>
