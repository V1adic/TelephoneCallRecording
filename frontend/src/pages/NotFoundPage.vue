<template>
  <div class="not-found">
    <p class="eyebrow">404</p>
    <h1>Маршрут не найден</h1>
    <p>Похоже, страница была перемещена или адрес введён неверно.</p>
    <RouterLink class="primary-button" to="/dashboard">Перейти в кабинет</RouterLink>
  </div>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>
