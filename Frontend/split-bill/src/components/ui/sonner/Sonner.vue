<script lang="ts" setup>
import type { ToasterProps } from "vue-sonner"
import { CircleCheckIcon, InfoIcon, Loader2Icon, CircleAlert, TriangleAlertIcon, XIcon } from "@lucide/vue"
import { cn } from "cn"
import { Toaster as Sonner } from "vue-sonner"

const props = defineProps<ToasterProps>()
</script>

<template>
  <Sonner
    :class="cn('toaster group', props.class)"
    :style="{
      '--normal-bg': 'var(--primary)',
      '--normal-text': 'var(--primary-foreground)',
      '--normal-border': 'var(--primary)',
      '--border-radius': 'var(--radius)',
      'font-family': 'inherit',
    }"
    v-bind="props"
    position="bottom-center"
    :toast-options="{
      classes: {
        title: 'text-base! font-bold!',
        description: 'text-sm! text-primary-foreground/75!',
        icon: 'size-5!',
      },
    }"
  >
    <template #success-icon>
      <CircleCheckIcon class="text-brand size-5" />
    </template>
    <template #info-icon>
      <InfoIcon class="size-4" />
    </template>
    <template #warning-icon>
      <TriangleAlertIcon class="size-4" />
    </template>
    <template #error-icon>
      <CircleAlert class="text-brand size-5" />
    </template>
    <template #loading-icon>
      <div>
        <Loader2Icon class="size-4 animate-spin" />
      </div>
    </template>
    <template #close-icon>
      <XIcon class="size-4" />
    </template>
  </Sonner>
</template>
