<template>
  <aside :class="wrapperClass">
    <div class="flex items-start gap-3">
      <div :class="dotClass" />
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.28em] text-white/75">
          {{ props.label }}
        </p>
        <p class="mt-2 text-sm leading-6 text-slate-100">
          {{ props.message }}
        </p>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  label: string
  message: string
  tone?: 'info' | 'success' | 'danger'
}>(), {
  tone: 'info'
})

const wrapperClass = computed(() => {
  const toneClass = {
    info: 'border-cyan-300/20 bg-cyan-300/10',
    success: 'border-emerald-300/20 bg-emerald-300/10',
    danger: 'border-rose-300/20 bg-rose-300/10'
  }

  return [
    'rounded-[1.4rem] border px-4 py-4 sm:px-5',
    toneClass[props.tone]
  ]
})

const dotClass = computed(() => {
  const toneClass = {
    info: 'bg-cyan-200',
    success: 'bg-emerald-300',
    danger: 'bg-rose-300'
  }

  return ['mt-1.5 h-2.5 w-2.5 rounded-full', toneClass[props.tone]]
})
</script>
