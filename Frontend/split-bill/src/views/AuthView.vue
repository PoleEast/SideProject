<script setup lang="ts">
import { ref } from 'vue'
import { Bed, Plane, ReceiptText, ShoppingBag, TramFront, Utensils } from '@lucide/vue'
import { Card, CardContent } from '@/components/ui/card'
import LoginForm from '@/components/auth/LoginForm.vue'
import RegisterForm from '@/components/auth/RegisterForm.vue'

const mode = ref<'login' | 'register'>('login')
const switchMode = () => {
  mode.value = mode.value === 'login' ? 'register' : 'login'
}
</script>

<template>
  <main class="bg-brand flex min-h-dvh flex-col justify-center p-4 relative">
    <svg class="text-foreground/10 absolute inset-0 size-full" aria-hidden="true">
      <defs>
        <pattern id="auth-pattern" width="720" height="416" patternUnits="userSpaceOnUse">
          <g transform="translate(60 96) rotate(-14)">
            <ReceiptText :size="88" x="-44" y="-44" stroke-width="1.5" />
          </g>
          <g transform="translate(300 112) rotate(16)">
            <TramFront :size="96" x="-48" y="-48" stroke-width="1.5" />
          </g>
          <g transform="translate(540 100) rotate(-10)">
            <ShoppingBag :size="84" x="-42" y="-42" stroke-width="1.5" />
          </g>
          <g transform="translate(180 320) rotate(12)">
            <Utensils :size="84" x="-42" y="-42" stroke-width="1.5" />
          </g>
          <g transform="translate(420 304) rotate(10)">
            <Bed :size="92" x="-46" y="-46" stroke-width="1.5" />
          </g>
          <g transform="translate(660 316) rotate(-18)">
            <Plane :size="88" x="-44" y="-44" stroke-width="1.5" />
          </g>
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#auth-pattern)" />
    </svg>
    <div class="relative mx-auto flex w-full max-w-sm flex-col gap-6">
      <!-- header -->
      <header class="flex flex-col gap-4 px-2">
        <span class="bg-primary text-brand flex size-14 items-center justify-center rounded-lg">
          <ReceiptText class="size-7" aria-hidden="true" />
        </span>
        <div class="flex flex-col gap-0.5">
          <h1 class="text-display font-bold tracking-tight">
            {{ mode === 'login' ? '歡迎回來' : '歡迎加入' }}
          </h1>
          <p class="text-base font-semibold">
            {{ mode === 'login' ? '登入後開始分帳' : '建立帳號，開始跟朋友分帳' }}
          </p>
        </div>
      </header>
      <!-- form -->
      <Card>
        <CardContent>
          <RegisterForm v-if="mode === 'register'" @switch="switchMode" />
          <LoginForm v-else @switch="switchMode" />
        </CardContent>
      </Card>
    </div>
  </main>
</template>
