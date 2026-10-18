<script setup lang="ts">
import { watch } from 'vue'
import { RouterView } from 'vue-router'

import { useAuthStore } from './stores/auth'
import AuthView from './views/AuthView.vue'

const authStore = useAuthStore()

watch(
  () => authStore.isLoggedIn,
  (isLoggedIn) => {
    if (isLoggedIn) {
      authStore.loadMe()
    }
  },
  {
    immediate: true,
  },
)
</script>

<template>
  <RouterView v-if="authStore.isLoggedIn" />
  <AuthView v-else />
</template>

<style scoped></style>
