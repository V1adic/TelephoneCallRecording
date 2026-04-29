<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto flex min-h-screen max-w-4xl items-center justify-center px-4 py-10 sm:px-6">
      <section class="panel-strong animate-rise w-full rounded-[1.75rem] px-6 py-10 text-center sm:px-8">
        <div class="icon-box mx-auto">
          <UiIcon name="home" :size="22" />
        </div>
        <p class="mt-5 text-sm font-bold text-blue-700">404</p>
        <h1 class="mt-3 text-5xl font-black tracking-tight text-slate-950 sm:text-6xl">Страница не найдена</h1>
        <p class="mx-auto mt-4 max-w-2xl text-base leading-7 text-slate-600">
          Возможно, адрес введён неверно или страница была перемещена. Вернитесь в кабинет и продолжите работу.
        </p>
        <div class="mt-8 flex justify-center">
          <RouterLink class="btn-primary" to="/dashboard">
            <UiIcon name="home" :size="18" />
            Перейти в кабинет
          </RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'
import UiIcon from '@/components/UiIcon.vue'
</script>
