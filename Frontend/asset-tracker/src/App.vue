<script setup lang="ts">
import {
  NConfigProvider,
  NMessageProvider,
  NNotificationProvider,
  NModalProvider,
  NDialogProvider,
  NLoadingBarProvider,
} from 'naive-ui'
import { RouterView } from 'vue-router'
import AuthModal from './components/AuthModal.vue'
import { useAuthStore } from './stores/auth'
import { computed } from 'vue'
import AppLayout from './components/AppLayout.vue'

const authStore = useAuthStore()
const showAuthModal = computed(() => !authStore.isLoggedIn)
</script>

<template>
  <n-config-provider>
    <n-loading-bar-provider>
      <n-notification-provider>
        <n-message-provider>
          <n-modal-provider>
            <n-dialog-provider>
              <auth-modal :show="showAuthModal" />
              <app-layout>
                <router-view v-if="authStore.isLoggedIn"></router-view>
              </app-layout>
            </n-dialog-provider>
          </n-modal-provider>
        </n-message-provider>
      </n-notification-provider>
    </n-loading-bar-provider>
  </n-config-provider>
</template>
