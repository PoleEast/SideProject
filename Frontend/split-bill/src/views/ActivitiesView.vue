<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <h1>Activities View</h1>
  <p>Current route: {{ route.path }}</p>
</template>
