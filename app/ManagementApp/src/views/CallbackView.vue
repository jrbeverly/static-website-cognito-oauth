<template>
  <v-container class="fill-height d-flex align-center justify-center">
    <template v-if="error">
      <v-alert type="error" :text="error" />
      <v-btn class="ml-3" @click="retry">Retry</v-btn>
    </template>
    <template v-else>
      <v-progress-circular indeterminate />
      <span class="ml-3">Completing sign-in...</span>
    </template>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const error = ref<string | null>(null)

async function handleCallback(): Promise<void> {
  const code = route.query.code as string | undefined
  const authError = route.query.error as string | undefined
  const state = route.query.state as string | undefined

  if (authError) {
    error.value = route.query.error_description as string || authError
    return
  }

  if (!code) {
    auth.login()
    return
  }

  try {
    await auth.handleCallback(code, state ?? '')
    router.replace({ name: 'home' })
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Authentication failed'
  }
}

function retry(): void {
  error.value = null
  auth.login()
}

onMounted(handleCallback)
</script>
