<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useAuthStore } from './stores/auth'
import AuthView from './views/AuthView.vue'

const authStore = useAuthStore()
</script>

<template>
  <RouterView v-if="authStore.isLoggedIn" />
  <AuthView v-else />
</template>

<style scoped></style>
