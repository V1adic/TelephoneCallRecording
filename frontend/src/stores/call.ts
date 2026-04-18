import { ref } from 'vue'
import { defineStore } from 'pinia'
import { endCall, fetchActiveCall, startCall, type ActiveCall, type CallEndResult, type CallStartResult } from '@/lib/api'

export const useCallStore = defineStore('call', () => {
  const activeCall = ref<ActiveCall | null>(null)
  const lastCompletedCall = ref<CallEndResult | null>(null)
  const isBusy = ref(false)

  async function loadActiveCall() {
    const response = await fetchActiveCall()
    activeCall.value = response.activeCall
    return activeCall.value
  }

  async function beginCall(destPhone: string) {
    isBusy.value = true
    try {
      const response = await startCall(destPhone)
      activeCall.value = response.destPhone && response.startedAtUtc && response.timeOfDay
        ? {
            destPhone: response.destPhone,
            startedAtUtc: response.startedAtUtc,
            timeOfDay: response.timeOfDay
          }
        : null
      lastCompletedCall.value = null
      return response as CallStartResult
    } finally {
      isBusy.value = false
    }
  }

  async function finishCall(destPhone: string) {
    isBusy.value = true
    try {
      const response = await endCall(destPhone)
      activeCall.value = null
      lastCompletedCall.value = response
      return response
    } finally {
      isBusy.value = false
    }
  }

  return {
    activeCall,
    lastCompletedCall,
    isBusy,
    loadActiveCall,
    beginCall,
    finishCall
  }
})
