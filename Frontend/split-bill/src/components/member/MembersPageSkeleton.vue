<script setup lang="ts">
import { LinkIcon } from '@lucide/vue'

import IconPattern from '@/components/icon/IconPattern.vue'
import { Skeleton } from '@/components/ui/skeleton'

const nameWidths = ['w-20', 'w-16', 'w-24', 'w-14']
</script>

<template>
  <main class="flex flex-1 flex-col gap-5 px-4 pt-1 pb-4">
    <section
      class="bg-brand dark:bg-card relative flex flex-col gap-3 overflow-hidden rounded-xl px-4 pt-4 pb-5"
    >
      <IconPattern :offset-x="40" :offset-y="-30" />
      <h2 class="relative flex h-9 items-center text-lg font-bold">邀請連結</h2>
      <div
        class="bg-secondary relative flex h-12 items-center gap-2.5 rounded-md px-3.5"
        aria-hidden="true"
      >
        <LinkIcon class="text-muted-foreground size-4.5 shrink-0" />
        <Skeleton class="bg-foreground/7 h-3.5 w-36 rounded-full" />
      </div>
      <div class="relative flex gap-2" aria-hidden="true">
        <Skeleton class="bg-foreground/12 h-12 flex-1 rounded-md" />
        <Skeleton class="bg-foreground/12 h-12 flex-1 rounded-md" />
      </div>
      <p class="dark:text-muted-foreground relative text-sm font-semibold">
        朋友點開連結、登入之後，就能認領自己的名字
      </p>
    </section>

    <section class="flex flex-col gap-2">
      <div class="flex items-center gap-1.5">
        <h2 class="text-lg font-bold">大家</h2>
        <Skeleton class="bg-foreground/7 h-3.5 w-4 rounded-full" aria-hidden="true" />
      </div>
      <div
        role="status"
        aria-label="載入中"
        class="bg-card shadow-card divide-y overflow-hidden rounded-xl"
      >
        <div
          v-for="(nameWidth, index) in nameWidths"
          :key="index"
          class="flex items-center gap-3 px-4 py-3"
          aria-hidden="true"
        >
          <Skeleton class="bg-foreground/7 size-10 shrink-0 rounded-full" />
          <div class="flex min-w-0 flex-1 flex-col gap-2">
            <Skeleton :class="['bg-foreground/7 h-3.5 rounded-full', nameWidth]" />
            <Skeleton class="bg-foreground/7 h-3 w-12 rounded-full" />
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
