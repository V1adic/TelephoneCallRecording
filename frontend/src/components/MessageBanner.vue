<template>
  <div :class="['message-banner', tone]">
    <span class="message-banner__label">{{ label }}</span>
    <p>{{ message }}</p>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  label: string
  message: string
  tone?: 'info' | 'success' | 'danger'
}>()
</script>
