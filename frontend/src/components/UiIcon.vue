<template>
  <svg
    aria-hidden="true"
    class="shrink-0"
    fill="none"
    :height="size"
    stroke="currentColor"
    stroke-linecap="round"
    stroke-linejoin="round"
    stroke-width="2"
    viewBox="0 0 24 24"
    :width="size"
  >
    <path v-for="path in iconPaths" :key="path" :d="path" />
  </svg>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const icons = {
  arrowLeft: ['M19 12H5', 'm12 19-7-7 7-7'],
  barChart: ['M4 19V5', 'M8 17v-5', 'M13 17V8', 'M18 17v-8', 'M4 19h16'],
  bell: ['M18 8a6 6 0 0 0-12 0c0 7-3 7-3 7h18s-3 0-3-7', 'M13.73 21a2 2 0 0 1-3.46 0'],
  calendar: ['M8 2v4', 'M16 2v4', 'M3 10h18', 'M5 4h14a2 2 0 0 1 2 2v13a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Z'],
  check: ['M20 6 9 17l-5-5'],
  city: ['M4 21V5a2 2 0 0 1 2-2h7v18', 'M13 8h5a2 2 0 0 1 2 2v11', 'M7 7h2', 'M7 11h2', 'M7 15h2', 'M16 12h1', 'M16 16h1'],
  clock: ['M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z', 'M12 7v5l3 2'],
  fileChart: ['M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z', 'M14 2v6h6', 'M8 18v-4', 'M12 18v-7', 'M16 18v-2'],
  history: ['M3 12a9 9 0 1 0 3-6.7', 'M3 4v5h5', 'M12 7v5l4 2'],
  home: ['M3 11 12 3l9 8', 'M5 10v10h14V10'],
  lock: ['M7 11V8a5 5 0 0 1 10 0v3', 'M6 11h12v10H6Z'],
  logOut: ['M10 17l5-5-5-5', 'M15 12H3', 'M21 19V5'],
  mail: ['M4 4h16v16H4Z', 'm22 6-10 7L2 6'],
  percent: ['M19 5 5 19', 'M7 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z', 'M17 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z'],
  phone: ['M22 16.92v3a2 2 0 0 1-2.18 2 19.8 19.8 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.12 4.18 2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.12.9.32 1.77.59 2.6a2 2 0 0 1-.45 2.11L8 9.69a16 16 0 0 0 6.31 6.31l1.26-1.25a2 2 0 0 1 2.11-.45c.83.27 1.7.47 2.6.59A2 2 0 0 1 22 16.92Z'],
  phoneCall: ['M22 16.92v3a2 2 0 0 1-2.18 2 19.8 19.8 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.12 4.18 2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.12.9.32 1.77.59 2.6a2 2 0 0 1-.45 2.11L8 9.69a16 16 0 0 0 6.31 6.31l1.26-1.25a2 2 0 0 1 2.11-.45c.83.27 1.7.47 2.6.59A2 2 0 0 1 22 16.92Z', 'M14 2a6 6 0 0 1 6 6', 'M14 6a2 2 0 0 1 2 2'],
  plus: ['M12 5v14', 'M5 12h14'],
  refresh: ['M21 12a9 9 0 0 1-15.5 6.2', 'M3 12A9 9 0 0 1 18.5 5.8', 'M18 2v4h4', 'M6 22v-4H2'],
  save: ['M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2Z', 'M17 21v-8H7v8', 'M7 3v5h8'],
  shield: ['M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z'],
  trash: ['M3 6h18', 'M8 6V4h8v2', 'M19 6l-1 14H6L5 6', 'M10 11v6', 'M14 11v6'],
  user: ['M20 21a8 8 0 0 0-16 0', 'M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z'],
  users: ['M16 21a6 6 0 0 0-12 0', 'M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M22 21a6 6 0 0 0-5-5.9', 'M17 3.3a4 4 0 0 1 0 7.4'],
  wallet: ['M20 7H5a2 2 0 0 1 0-4h13v4', 'M3 7v12a2 2 0 0 0 2 2h15V7', 'M16 14h4']
} as const

type IconName = keyof typeof icons

const props = withDefaults(defineProps<{
  name: IconName
  size?: number
}>(), {
  size: 20
})

const iconPaths = computed(() => icons[props.name])
</script>
