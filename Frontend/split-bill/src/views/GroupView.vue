<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <h1>Group View</h1>
  <p>Current route: {{ route.params.groupId }}</p>
</template>
