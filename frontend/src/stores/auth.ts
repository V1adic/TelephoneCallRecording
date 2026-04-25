import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError, clearCsrfToken, fetchProfile, getCsrfToken, login, logout, register, requestConfirmation, confirmEmail, type LoginPayload, type Profile, type RegisterPayload } from '@/lib/api'

export const useAuthStore = defineStore('auth', () => {
  const profile = ref<Profile | null>(null)
  const isBootstrapped = ref(false)
  const isBootstrapping = ref(false)

  const isAuthenticated = computed(() => profile.value !== null)

  let bootstrapPromise: Promise<void> | null = null
  
  async function bootstrap() {
    if (isBootstrapped.value) {
      return
    }

    if (bootstrapPromise) {
      return bootstrapPromise // ← ждём уже запущенный bootstrap
    }

    isBootstrapping.value = true

    bootstrapPromise = (async () => {
      try {
        await getCsrfToken()

        try {
          const data = await fetchProfile()
          profile.value = data
        } catch (error) {
          if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
            profile.value = null
          } else {
            profile.value = null
          }
        }

        isBootstrapped.value = true
      } catch (error) {
        profile.value = null
      } finally {
        isBootstrapping.value = false
        bootstrapPromise = null
      }
    })()

    return bootstrapPromise
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
    try {
      await logout()
    } catch (error) {
      if (!(error instanceof ApiError) || (error.status !== 401 && error.status !== 403)) {
        throw error
      }
    }

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
