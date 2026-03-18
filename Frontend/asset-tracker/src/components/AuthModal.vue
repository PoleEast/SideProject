<script setup lang="ts">
import {
  NAlert,
  NButton,
  NDivider,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NCard,
  NText,
  NH2,
  NP,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { useMessage } from 'naive-ui'
import type { FormInst, FormRules } from 'naive-ui'
import { login, register } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ show: boolean }>()

const authStore = useAuthStore()
const message = useMessage()

const formRef = ref<FormInst | null>(null)

const isLogin = ref(true)
const errorMsg = ref('')
const loading = ref(false)

const rules: FormRules = {
  account: [
    {
      required: true,
      max: 30,
      min: 6,
      message: '帳號長度請為6個字以上',
      trigger: ['blur', 'input'],
    },
  ],
  password: [
    {
      required: true,
      max: 30,
      min: 6,
      message: '密碼長度請為6個字以上',
      trigger: ['blur', 'input'],
    },
  ],
  reenteredPassword: [
    { required: true, message: '請輸入確認密碼', trigger: 'blur' },
    {
      validator: (rule, value) => value === formData.password,
      message: '密碼輸入不相同',
      trigger: 'blur',
    },
  ],
  name: [
    {
      required: true,
      min: 2,
      max: 30,
      message: '名稱長度請為2個字以上',
      trigger: ['blur', 'input'],
    },
  ],
}

const formData = reactive({
  account: '',
  password: '',
  reenteredPassword: '',
  name: '',
})

function switchMode() {
  isLogin.value = !isLogin.value
  formData.password = ''
  formData.reenteredPassword = ''
  errorMsg.value = ''
  formRef.value?.restoreValidation()
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }
  loading.value = true

  try {
    const response = isLogin.value
      ? await login({ account: formData.account, password: formData.password })
      : await register({
          account: formData.account,
          password: formData.password,
          name: formData.name,
        })

    if (response.ok) {
      authStore.setToken(response.data.token)
      message.success(isLogin.value ? '登入成功' : '註冊成功')
      return
    }

    errorMsg.value = response.message
  } catch {
    errorMsg.value = '網路連線發生問題，請稍後再試'
  } finally {
    loading.value = false
  }
}

const clearReenteredPassword = () => {
  formData.reenteredPassword = ''
}
</script>

<template>
  <n-modal :show="props.show" :mask-closable="false" :close-on-esc="false" style="width: 460px">
    <n-card class="rounded-2xl!">
      <!-- 標題 -->
      <div class="pt-4 pb-2 text-center">
        <n-h2 class="mb-1!">
          <n-text type="primary">{{ isLogin ? '登入' : '加入' }} Asset Tracker</n-text>
        </n-h2>
        <n-p depth="3" class="mt-0!">
          {{
            isLogin ? '歡迎回來，準備追蹤你的資產變化了嗎？' : '加入我們，開始記錄你的投資組合吧！'
          }}
        </n-p>
      </div>

      <!-- 登入表單 -->
      <n-form ref="formRef" v-if="isLogin" :model="formData" class="mt-2" :rules="rules">
        <n-form-item label="帳號" path="account">
          <n-input v-model:value="formData.account" placeholder="請輸入帳號" round maxlength="30" />
        </n-form-item>
        <n-form-item label="密碼" path="password">
          <n-input
            v-model:value="formData.password"
            type="password"
            placeholder="請輸入密碼"
            show-password-on="click"
            round
            @keyup.enter="handleSubmit()"
          />
        </n-form-item>
      </n-form>

      <!-- 註冊表單 -->
      <n-form ref="formRef" v-else class="mt-2" :model="formData" :rules="rules">
        <n-form-item label="帳號" path="account">
          <n-input v-model:value="formData.account" placeholder="請輸入帳號" round />
        </n-form-item>
        <n-form-item label="名稱" path="name">
          <n-input v-model:value="formData.name" placeholder="請輸入顯示名稱" round />
        </n-form-item>
        <n-form-item label="密碼" path="password">
          <n-input
            v-model:value="formData.password"
            type="password"
            placeholder="請輸入密碼"
            show-password-on="click"
            round
            @keyup.enter="handleSubmit()"
            @change="clearReenteredPassword"
          />
        </n-form-item>
        <n-form-item label="確認密碼" path="reenteredPassword">
          <n-input
            :disabled="formData.password === ''"
            v-model:value="formData.reenteredPassword"
            type="password"
            placeholder="請輸入密碼"
            show-password-on="click"
            round
            @keyup.enter="handleSubmit()"
          />
        </n-form-item>
      </n-form>

      <!-- 錯誤訊息 -->
      <n-alert v-if="errorMsg" type="error" class="mb-3" :bordered="false">
        {{ errorMsg }}
      </n-alert>

      <!-- 送出按鈕 -->
      <n-button type="primary" block round :loading="loading" @click="handleSubmit()">
        {{ isLogin ? '登入' : '註冊' }}
      </n-button>

      <!-- 分隔線 -->
      <n-divider class="my-4!">或</n-divider>

      <!-- 切換模式 -->
      <div class="text-center">
        <n-text type="primary">
          {{ isLogin ? '竟然還沒有帳號？' : '已經有帳號了？' }}
        </n-text>
        <n-button text type="primary" class="ml-2" @click="switchMode">
          {{ isLogin ? '立即加入！' : '立即登入！' }}
        </n-button>
      </div>
    </n-card>
  </n-modal>
</template>
