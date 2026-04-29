<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto flex min-h-screen max-w-3xl items-center justify-center px-4 py-6 sm:px-6">
      <section class="panel-surface animate-rise w-full overflow-hidden rounded-[1.75rem] px-6 py-8 sm:px-8 lg:px-10">
        <div class="mb-8 flex min-w-0 items-center gap-3">
          <div class="icon-box">
            <UiIcon name="phoneCall" :size="22" />
          </div>
          <div class="min-w-0">
            <p class="text-sm font-bold text-blue-700">Запись телефонных звонков</p>
            <p class="text-xs font-medium text-slate-500">Кабинет абонента и администратора</p>
          </div>
        </div>

        <slot />
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import UiIcon from '@/components/UiIcon.vue'

defineProps<{
  title: string
  lead: string
}>()
</script>
