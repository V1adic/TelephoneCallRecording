<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto flex min-h-screen max-w-4xl items-center justify-center px-4 py-10 sm:px-6">
      <section class="panel-strong animate-rise w-full rounded-[2rem] px-6 py-8 text-center sm:px-8">
        <p class="text-sm uppercase tracking-[0.34em] text-cyan-200/80">404</p>
        <h1 class="display-font mt-5 text-5xl text-white sm:text-6xl">Маршрут не найден</h1>
        <p class="mx-auto mt-4 max-w-2xl text-base leading-7 text-slate-300">
          Похоже, страница была перемещена или адрес введён неверно. Вернитесь в кабинет и продолжите работу из защищённого контура.
        </p>
        <div class="mt-8 flex justify-center">
          <RouterLink class="btn-primary" to="/dashboard">Перейти в кабинет</RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>
