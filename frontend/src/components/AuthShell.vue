<template>
  <div class="app-backdrop min-h-screen">
    <div class="mx-auto grid min-h-screen max-w-7xl grid-cols-1 gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[1.12fr_0.88fr] lg:px-8">
      <section class="panel-strong relative rounded-[2rem] px-6 py-8 sm:px-8 lg:px-10">
        <div class="aurora-orb absolute -left-16 bottom-8 h-56 w-56 rounded-full bg-cyan-300/15 blur-3xl" />
        <div class="aurora-orb absolute right-0 top-0 h-64 w-64 rounded-full bg-amber-300/12 blur-3xl" />

        <div class="relative flex h-full flex-col justify-between gap-10">
          <div>
            <div class="flex flex-wrap gap-2">
              <span class="badge-shell text-xs uppercase tracking-[0.22em] text-cyan-100">
                TelephoneCallRecording
              </span>
              <span class="badge-shell text-xs uppercase tracking-[0.22em] text-slate-200">
                Trusted Browser Path
              </span>
            </div>

            <h1 class="display-font mt-8 max-w-[11ch] text-5xl leading-none text-white sm:text-6xl lg:text-7xl">
              {{ title }}
            </h1>
            <p class="mt-5 max-w-2xl text-base leading-7 text-slate-300 sm:text-lg">
              {{ lead }}
            </p>
          </div>

          <div class="grid gap-3 sm:grid-cols-3">
            <article class="rounded-[1.4rem] border border-white/10 bg-white/5 p-4">
              <p class="text-xs uppercase tracking-[0.24em] text-cyan-100/80">Auth</p>
              <p class="mt-3 text-sm leading-6 text-slate-300">
                Cookie-сессия, CSRF и блокировка после серий неудачных попыток.
              </p>
            </article>

            <article class="rounded-[1.4rem] border border-white/10 bg-white/5 p-4">
              <p class="text-xs uppercase tracking-[0.24em] text-cyan-100/80">Billing</p>
              <p class="mt-3 text-sm leading-6 text-slate-300">
                Тарифы, скидки и расчёт стоимости по времени суток сервера.
              </p>
            </article>

            <article class="rounded-[1.4rem] border border-white/10 bg-white/5 p-4">
              <p class="text-xs uppercase tracking-[0.24em] text-cyan-100/80">Control</p>
              <p class="mt-3 text-sm leading-6 text-slate-300">
                Роли администратора и клиента с отдельными сценариями работы.
              </p>
            </article>
          </div>
        </div>
      </section>

      <section class="panel-surface rounded-[2rem] px-6 py-8 sm:px-8 lg:px-10">
        <slot />
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  lead: string
}>()
</script>
