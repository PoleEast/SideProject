<script setup lang="ts">
import { ref, reactive } from 'vue'

import { CircleAlert } from '@lucide/vue'

import { useRegle } from '@regle/core'
import { required, withMessage } from '@regle/rules'

import { Field, FieldGroup, FieldLabel, FieldError } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Spinner } from '@/components/ui/spinner'

import { login } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits<{ switch: [] }>()

const authStore = useAuthStore()

const formData = reactive({
  account: '',
  password: '',
})
const errorMessage = ref('')
const isSubmitting = ref(false)

const { r$ } = useRegle(
  formData,
  {
    account: { required: withMessage(required, '請輸入帳號') },
    password: { required: withMessage(required, '請輸入密碼') },
  },
  { autoDirty: false },
)

const handleSubmit = async () => {
  const { valid } = await r$.$validate()
  if (!valid) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const result = await login(formData)

    if (!result.ok) {
      errorMessage.value = result.message || '帳號或密碼錯誤'
      return
    }

    authStore.setToken(result.data.token)
  } catch {
    errorMessage.value = '登入失敗，請稍後再試'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <form @submit.prevent="handleSubmit">
    <FieldGroup>
      <Field :data-invalid="r$.account.$error">
        <FieldLabel for="account">帳號</FieldLabel>
        <Input
          id="account"
          v-model="formData.account"
          type="text"
          autoComplete="username"
          placeholder="請輸入帳號"
          :aria-invalid="r$.account.$error"
          @blur="r$.account.$touch()"
          @focus="r$.account.$reset()"
        />
        <FieldError :errors="r$.account.$errors" />
      </Field>
      <Field :data-invalid="r$.password.$error">
        <FieldLabel for="password">密碼</FieldLabel>
        <Input
          id="password"
          v-model="formData.password"
          type="password"
          autoComplete="current-password"
          placeholder="請輸入密碼"
          :aria-invalid="r$.password.$error"
          @blur="r$.password.$touch()"
          @focus="r$.password.$reset()"
        />
        <FieldError :errors="r$.password.$errors" />
      </Field>
      <Alert v-if="errorMessage" variant="destructive">
        <CircleAlert />
        <AlertDescription>
          {{ errorMessage }}
        </AlertDescription>
      </Alert>
      <Field class="mt-2">
        <Button type="submit" size="lg" :disabled="isSubmitting">
          <Spinner v-if="isSubmitting" />
          {{ isSubmitting ? '登入中...' : '登入' }}
        </Button>
        <p class="text-muted-foreground flex items-center justify-center text-sm">
          還沒有帳號？
          <Button
            type="button"
            variant="link"
            class="h-11 px-1 font-semibold underline"
            @click="emit('switch')"
          >
            註冊一個
          </Button>
        </p>
      </Field>
    </FieldGroup>
  </form>
</template>
