import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError, clearCsrfToken, fetchProfile, getCsrfToken, login, logout, register, requestConfirmation, confirmEmail, type LoginPayload, type Profile, type RegisterPayload } from '@/lib/api'

export const useAuthStore = defineStore('auth', () => {
  const profile = ref<Profile | null>(null)
  const isBootstrapped = ref(false)
  const isBootstrapping = ref(false)

  const isAuthenticated = computed(() => profile.value !== null)

  async function bootstrap() {
    if (isBootstrapped.value || isBootstrapping.value) {
      return
    }

    isBootstrapping.value = true
    try {
      await getCsrfToken()
      try {
        profile.value = await fetchProfile()
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 401) {
          throw error
        }
        profile.value = null
      }
      isBootstrapped.value = true
    } finally {
      isBootstrapping.value = false
    }
  }

  async function signIn(payload: LoginPayload) {
    const nextProfile = await login(payload)
    await getCsrfToken()
    profile.value = nextProfile
    isBootstrapped.value = true
    return nextProfile
  }

  async function signUp(payload: RegisterPayload) {
    return register(payload)
  }

  async function resendConfirmation() {
    return requestConfirmation()
  }

  async function completeConfirmation(code: string) {
    return confirmEmail(code)
  }

  async function refreshProfile() {
    profile.value = await fetchProfile()
    return profile.value
  }

  async function signOut() {
    await logout()
    clearCsrfToken()
    await getCsrfToken()
    profile.value = null
  }

  return {
    profile,
    isBootstrapped,
    isBootstrapping,
    isAuthenticated,
    bootstrap,
    signIn,
    signUp,
    resendConfirmation,
    completeConfirmation,
    refreshProfile,
    signOut
  }
})
