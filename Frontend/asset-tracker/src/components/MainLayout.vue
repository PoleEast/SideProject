<script setup lang="ts">
import {
  NFlex,
  NImage,
  NLayout,
  NLayoutContent,
  NLayoutHeader,
  NLayoutSider,
  NText,
} from 'naive-ui'
import UserButton from './UserButton.vue'
import { RouterView } from 'vue-router'
import LoginModal from './LoginModal.vue'
</script>

<template>
  <n-layout>
    <n-layout-header bordered class="px-5">
      <n-flex justify="space-between" align="center">
        <n-text>
          <n-flex align="center">
            <n-image width="50" src="/logoLight.svg" />
            <h3>TrackFin</h3>
          </n-flex>
        </n-text>
        <User-button />
      </n-flex>
    </n-layout-header>
    <n-layout-sider></n-layout-sider>
    <n-layout-content>
      <LoginModal />
      <router-view />
    </n-layout-content>
  </n-layout>
</template>
