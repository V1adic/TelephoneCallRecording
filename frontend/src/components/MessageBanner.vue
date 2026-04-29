<template>
  <aside :class="wrapperClass">
    <div class="flex items-start gap-3">
      <div :class="iconClass">
        <UiIcon :name="iconName" :size="18" />
      </div>
      <div>
        <p class="text-xs font-bold uppercase tracking-[0.08em] text-slate-700">
          {{ props.label }}
        </p>
        <p class="mt-1.5 text-sm leading-6 text-slate-700">
          {{ props.message }}
        </p>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import UiIcon from '@/components/UiIcon.vue'

const props = withDefaults(defineProps<{
  label: string
  message: string
  tone?: 'info' | 'success' | 'danger'
}>(), {
  tone: 'info'
})

const wrapperClass = computed(() => {
  const toneClass = {
    info: 'border-blue-200 bg-blue-50',
    success: 'border-emerald-200 bg-emerald-50',
    danger: 'border-rose-200 bg-rose-50'
  }

  return [
    'rounded-[1.25rem] border px-4 py-4 shadow-sm sm:px-5',
    toneClass[props.tone]
  ]
})

const iconClass = computed(() => {
  const toneClass = {
    info: 'bg-blue-100 text-blue-700',
    success: 'bg-emerald-100 text-emerald-700',
    danger: 'bg-rose-100 text-rose-700'
  }

  return ['flex h-9 w-9 shrink-0 items-center justify-center rounded-full', toneClass[props.tone]]
})

const iconName = computed(() => {
  const names = {
    info: 'bell',
    success: 'check',
    danger: 'bell'
  } as const

  return names[props.tone]
})
</script>
