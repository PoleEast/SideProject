<script setup lang="ts">
import { NCard, NModal } from 'naive-ui'

const model = true
const isLoginDialog: boolean = true
</script>

<template>
  <n-modal :show="model">
    <n-card class="w-3xs">
      <template #header>
        <span>
          <n-icon>
            <LogInRound />
          </n-icon>
          {{ isLoginDialog ? '登入' : '加入' }} TrackFin
        </span>
        <span>{{ isLoginDialog ? 'Welcome back!' : 'Join us!' }}</span>
      </template>
      <template #action></template>
    </n-card>
  </n-modal>
</template>
