<script setup lang="ts">
import {
  NModal,
  NCard,
  NH3,
  NText,
  NForm,
  NFormItem,
  NInput,
  NDatePicker,
  NInputNumber,
  NButton,
  NRadioGroup,
  NRadioButton,
  NIcon,
  type FormRules,
  type FormInst,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref, watch } from 'vue'
import { EditRound, AddOutlined } from '@vicons/material'
import type { TransactionRequest, TransactionResponse } from '@/types/transaction'
import { create, updateTransaction } from '@/api/transaction'
import { marketColors, transactionTypeColors } from '@/utils/colors'
import type { MarketType, CurrencyType } from '@/types/common'

const props = defineProps<{
  transaction: TransactionResponse | null
}>()
const show = defineModel<boolean>('show')
const emit = defineEmits<{ refresh: [] }>()

const message = useMessage()

const isEdit = computed(() => props.transaction !== null)
const formRef = ref<FormInst | null>(null)
const formData = reactive({
  stockCode: '',
  market: 'TW' as MarketType,
  date: Date.now() as number | null,
  type: null as string | null,
  price: null as number | null,
  quantity: null as number | null,
  currency: 'TWD' as CurrencyType,
  remark: '',
})
const rules: FormRules = {
  stockCode: [{ required: true, message: '請輸入股票代碼', trigger: ['blur', 'input'] }],
  market: [{ required: true, message: '請選擇交易市場', trigger: ['blur', 'change'] }],
  date: [
    { required: true, type: 'number', message: '請選擇交易日期', trigger: ['blur', 'change'] },
  ],
  type: [{ required: true, message: '請選擇交易類型', trigger: ['blur', 'change'] }],
  price: [{ required: true, type: 'number', message: '請輸入價格', trigger: ['blur', 'input'] }],
  quantity: [{ required: true, type: 'number', message: '請輸入數量', trigger: ['blur', 'input'] }],
  currency: [{ required: true, message: '請選擇計價幣別', trigger: ['blur', 'change'] }],
}
const loading = ref(false)

const markets = (['TW', 'US', 'JP'] as MarketType[]).map((value) => ({
  value,
  style: {
    '--n-button-text-color-active': marketColors[value].secondary,
    '--n-button-border-color-active': marketColors[value].secondary,
    '--n-button-color-active': marketColors[value].primary,
    '--n-button-box-shadow-focus': `inset 0 0 0 1px ${marketColors[value].secondary}, 0 0 0 2px ${marketColors[value].secondary}33`,
  },
}))

const currencies = ['TWD', 'USD', 'JPY']

const handleSubmit = async () => {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  loading.value = true

  try {
    const payload = {
      ...formData,
      date: new Date(formData.date as number).toISOString(),
    } as TransactionRequest
    const response = isEdit.value
      ? await updateTransaction(props.transaction!.id, payload)
      : await create(payload)

    if (response.ok) {
      message.success(`${isEdit.value ? '編輯交易資料成功' : '新增交易資料成功'}`)
      show.value = false
      emit('refresh')
    }
  } catch {
    message.error('網路連線發生問題，請稍後再試')
  } finally {
    loading.value = false
  }
}

// modal 開啟時同步表單資料：編輯模式填入既有資料，新增模式重置表單
watch(
  () => props.transaction,
  (val) => {
    if (val) {
      formData.stockCode = val.stockCode
      formData.market = val.market
      formData.date = new Date(val.date).getTime()
      formData.type = val.type
      formData.price = val.price
      formData.quantity = val.quantity
      formData.currency = val.currency
      formData.remark = val.remark
    } else {
      formData.stockCode = ''
      formData.market = 'TW'
      formData.date = Date.now()
      formData.type = null
      formData.price = null
      formData.quantity = null
      formData.currency = 'TWD'
      formData.remark = ''
    }
  },
)
</script>

<template>
  <n-modal v-model:show="show" style="width: 520px" @mask-click="show = false">
    <n-card class="overflow-hidden! rounded-2xl!" :content-style="'padding: 24px'">
      <!-- Header -->
      <div
        class="-mx-6 -mt-6 mb-6 px-6 py-5 transition-colors duration-300"
        :class="{
          'bg-red-50': formData.type === 'Buy',
          'bg-green-50': formData.type === 'Sell',
          'bg-gray-50': formData.type === null,
        }"
      >
        <div
          class="mb-1 text-xs font-semibold tracking-widest uppercase transition-colors duration-300"
          :class="{
            'text-red-500': formData.type === 'Buy',
            'text-green-500': formData.type === 'Sell',
            'text-gray-400': formData.type === null,
          }"
        >
          {{
            formData.type === 'Buy'
              ? 'Buy Order'
              : formData.type === 'Sell'
                ? 'Sell Order'
                : 'New Order'
          }}
        </div>
        <div class="flex items-end justify-between">
          <n-h3 class="m-0! flex items-center gap-2 font-bold">
            <n-icon size="22">
              <EditRound v-if="isEdit" />
              <AddOutlined v-else />
            </n-icon>
            <n-text>
              {{ isEdit ? '編輯交易' : '新增交易' }}
            </n-text>
          </n-h3>

          <!-- 市場選擇器（放 header 右側） -->
          <n-radio-group v-model:value="formData.market">
            <n-radio-button
              v-for="market in markets"
              :key="market.value"
              :value="market.value"
              :style="market.style"
            >
              {{ market.value }}
            </n-radio-button>
          </n-radio-group>
        </div>
      </div>

      <n-form
        ref="formRef"
        :model="formData"
        :rules="rules"
        label-placement="left"
        label-width="90"
      >
        <n-form-item label="股票代碼" path="stockCode">
          <n-input
            v-model:value="formData.stockCode"
            placeholder="例：2330、AAPL"
            :maxlength="5"
            @input="formData.stockCode = formData.stockCode.trim().toUpperCase()"
          />
        </n-form-item>

        <!-- 交易日期 -->
        <n-form-item label="交易日期" path="date">
          <n-date-picker
            v-model:value="formData.date"
            type="date"
            placeholder="請選擇日期"
            class="w-full"
            :is-date-disabled="(value: number) => value > Date.now()"
          />
        </n-form-item>

        <!-- 交易類型 -->
        <n-form-item label="交易類型" path="type">
          <n-radio-group v-model:value="formData.type" class="w-full">
            <n-radio-button
              value="Buy"
              class="w-1/2 text-center"
              :style="{
                '--n-button-text-color-active': transactionTypeColors.Buy.primary,
                '--n-button-border-color-active': transactionTypeColors.Buy.primary,
                '--n-button-color-active': `${transactionTypeColors.Buy.primary}1a`,
                '--n-button-box-shadow-focus': `inset 0 0 0 1px ${transactionTypeColors.Buy.primary}, 0 0 0 2px ${transactionTypeColors.Buy.primary}4d`,
              }"
              >買入 Buy</n-radio-button
            >
            <n-radio-button
              value="Sell"
              class="w-1/2 text-center"
              :style="{
                '--n-button-text-color-active': transactionTypeColors.Sell.primary,
                '--n-button-border-color-active': transactionTypeColors.Sell.primary,
                '--n-button-color-active': `${transactionTypeColors.Sell.primary}1a`,
                '--n-button-box-shadow-focus': `inset 0 0 0 1px ${transactionTypeColors.Sell.primary}, 0 0 0 2px ${transactionTypeColors.Sell.primary}4d`,
              }"
              >賣出 Sell</n-radio-button
            >
          </n-radio-group>
        </n-form-item>

        <!-- 價格 + 數量並排 -->
        <div class="flex">
          <n-form-item label="價格" path="price" class="flex-1">
            <n-input-number
              v-model:value="formData.price"
              placeholder="每單位價格"
              :min="0.01"
              class="w-full"
            />
          </n-form-item>
          <n-form-item label="數量" path="quantity" class="flex-1">
            <n-input-number
              v-model:value="formData.quantity"
              placeholder="交易股數"
              :min="1"
              :precision="0"
              class="w-full"
            />
          </n-form-item>
        </div>

        <!-- 貨幣 -->
        <n-form-item label="計價幣別" path="currency">
          <n-radio-group v-model:value="formData.currency" class="w-full">
            <n-radio-button
              v-for="currency in currencies"
              :key="currency"
              :value="currency"
              class="w-1/3 text-center"
            >
              {{ currency }}
            </n-radio-button>
          </n-radio-group>
        </n-form-item>

        <!-- 備註 -->
        <n-form-item label="備註" path="remark">
          <n-input
            v-model:value="formData.remark"
            type="textarea"
            placeholder="選填"
            maxlength="200"
            :autosize="{ minRows: 1, maxRows: 4 }"
          />
        </n-form-item>
      </n-form>

      <!-- 按鈕 -->
      <div class="mt-4 flex gap-3">
        <n-button class="flex-1" size="large" secondary @click="show = false">取消</n-button>
        <n-button
          class="flex-1"
          size="large"
          type="primary"
          :loading="loading"
          @click="handleSubmit"
        >
          {{ isEdit ? '儲存變更' : '新增' }}
        </n-button>
      </div>
    </n-card>
  </n-modal>
</template>
