<template>
  <v-app-bar color="primary" density="compact">
    <v-app-bar-title>My Sites</v-app-bar-title>
    <v-spacer />
    <template v-if="authStore.isAuthenticated">
      <span class="text-caption mr-3">{{ authStore.email }}</span>
      <v-btn variant="text" @click="authStore.logout()">
        Sign Out
      </v-btn>
    </template>
    <v-btn v-else variant="text" @click="authStore.login()">
      Sign In
    </v-btn>
  </v-app-bar>
  <v-main>
    <v-container>
      <router-view />
    </v-container>
  </v-main>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>
