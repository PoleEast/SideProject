<script setup lang="ts">
import { reactive, ref } from 'vue'

import { CircleAlert } from '@lucide/vue'

import { useRegle } from '@regle/core'
import { maxLength, minLength, required, withMessage } from '@regle/rules'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'

import { register } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits<{ switch: [] }>()

const authStore = useAuthStore()

const formData = reactive({
  name: '',
  account: '',
  password: '',
})
const errorMessage = ref('')
const isSubmitting = ref(false)

const { r$ } = useRegle(
  formData,
  {
    name: {
      required: withMessage(required, '請輸入名字'),
      minLength: withMessage(minLength(2), '名字至少要兩個字'),
      maxLength: withMessage(maxLength(32), '名字最多32個字'),
    },
    account: {
      required: withMessage(required, '請輸入帳號'),
      minLength: withMessage(minLength(6), '帳號至少要6個字'),
      maxLength: withMessage(maxLength(32), '帳號最多32個字'),
    },
    password: {
      required: withMessage(required, '請輸入密碼'),
      minLength: withMessage(minLength(6), '密碼至少要6個字'),
      maxLength: withMessage(maxLength(30), '密碼最多30個字'),
    },
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
    const result = await register(formData)
    if (!result.ok) {
      errorMessage.value = result.message || '註冊失敗，請稍後再試'
      return
    }
    authStore.setToken(result.data.token)
  } catch {
    errorMessage.value = '註冊失敗，請稍後再試'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <form @submit.prevent="handleSubmit">
    <FieldGroup>
      <Field :data-invalid="r$.name.$error">
        <FieldLabel for="name">名字</FieldLabel>
        <Input
          id="name"
          v-model="formData.name"
          type="text"
          autocomplete="nickname"
          placeholder="例如：小安"
          :aria-invalid="r$.name.$error"
          @blur="r$.name.$touch()"
          @focus="r$.name.$reset()"
        />
        <FieldError v-if="r$.name.$error" class="ps-1.5" :errors="r$.name.$errors" />
        <FieldDescription v-else class="ps-1.5"> 朋友在群組裡會看到這個名字 </FieldDescription>
      </Field>
      <Field :data-invalid="r$.account.$error">
        <FieldLabel for="account">帳號</FieldLabel>
        <Input
          id="account"
          v-model="formData.account"
          type="text"
          autocomplete="username"
          placeholder="請輸入帳號"
          :aria-invalid="r$.account.$error"
          @blur="r$.account.$touch()"
          @focus="r$.account.$reset()"
        />
        <FieldError v-if="r$.account.$error" class="ps-1.5" :errors="r$.account.$errors" />
        <FieldDescription v-else class="ps-1.5"> 6～32 個字，之後登入時用 </FieldDescription>
      </Field>
      <Field :data-invalid="r$.password.$error">
        <FieldLabel for="password">密碼</FieldLabel>
        <Input
          id="password"
          v-model="formData.password"
          type="password"
          autocomplete="new-password"
          placeholder="請輸入密碼"
          :aria-invalid="r$.password.$error"
          @blur="r$.password.$touch()"
          @focus="r$.password.$reset()"
        />
        <FieldError v-if="r$.password.$error" class="ps-1.5" :errors="r$.password.$errors" />
        <FieldDescription v-else class="ps-1.5"> 6～30 個字 </FieldDescription>
      </Field>
      <Alert v-if="errorMessage" variant="destructive">
        <CircleAlert />
        <AlertDescription>{{ errorMessage }}</AlertDescription>
      </Alert>
      <Field class="mt-2">
        <Button type="submit" size="lg" :disabled="isSubmitting">
          <Spinner v-if="isSubmitting" />
          {{ isSubmitting ? '註冊中...' : '註冊' }}
        </Button>
        <p class="text-muted-foreground flex items-center justify-center text-sm">
          已經有帳號？
          <Button
            type="button"
            variant="link"
            class="h-11 px-1 font-semibold underline"
            @click="emit('switch')"
          >
            登入
          </Button>
        </p>
      </Field>
    </FieldGroup>
  </form>
</template>
