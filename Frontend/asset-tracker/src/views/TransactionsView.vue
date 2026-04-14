<script setup lang="ts">
import { computed, h, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import {
  NButton,
  NDataTable,
  NTag,
  NSpace,
  NSelect,
  NInput,
  NFlex,
  NH2,
  NText,
  NEmpty,
  NPopconfirm,
  NIcon,
  NAlert,
  NSpin,
  useMessage,
} from 'naive-ui'
import type { DataTableColumns, SelectOption } from 'naive-ui'

import { AddRound, EditRound, DeleteRound, SearchRound } from '@vicons/material'

import TransactionModal from '@/components/TransactionModal.vue'
import type { TransactionResponse } from '@/types/transaction'
import { deleteTransaction, getTransactions } from '@/api/transaction'
import { marketColors } from '@/utils/colors'

const message = useMessage()
const route = useRoute()

const transactions = ref<TransactionResponse[]>([])
const errorMessage = ref<string | null>(null)
const isLoading = ref(false)

// 篩選
const filterMarket = ref<string | null>((route.query.stockMarket as string) ?? null)
const filterType = ref<string | null>(null)
const filterCode = ref((route.query.stockCode as string) ?? '')

const marketOptions: SelectOption[] = [
  { label: '台股 TW', value: 'TW' },
  { label: '美股 US', value: 'US' },
  { label: '日股 JP', value: 'JP' },
]

const typeOptions: SelectOption[] = [
  { label: '全部類型', value: undefined },
  { label: '買入', value: 'Buy' },
  { label: '賣出', value: 'Sell' },
]

const filterTransactions = computed<TransactionResponse[]>(() => {
  const marketFilter = (transaction: TransactionResponse) => {
    return !filterMarket.value || transaction.market === filterMarket.value
  }

  const typeFilter = (transaction: TransactionResponse) => {
    return !filterType.value || transaction.type === filterType.value
  }

  const codeFilter = (transaction: TransactionResponse) => {
    return (
      !filterCode.value ||
      transaction.stockCode.toUpperCase().includes(filterCode.value.toUpperCase())
    )
  }

  return transactions.value.filter(
    (transaction) =>
      marketFilter(transaction) && typeFilter(transaction) && codeFilter(transaction),
  )
})

// Modal
const showModal = ref(false)
const editingTransaction = ref<TransactionResponse | null>(null)

const openCreate = () => {
  editingTransaction.value = null
  showModal.value = true
}

const openEdit = (transaction: TransactionResponse) => {
  editingTransaction.value = transaction
  showModal.value = true
}

const handleDelete = async (id: number) => {
  try {
    const result = await deleteTransaction(id)

    if (!result.ok) {
      message.error(result.message)
      return
    }

    loadTransactions()
  } catch {
    message.error('網路連線發生問題刪除失敗，請稍後再試')
  }
}

// Table columns
const columns: DataTableColumns<TransactionResponse> = [
  { title: '日期', key: 'date', width: 110, render: (row) => row.date.slice(0, 10) },
  {
    title: '市場',
    key: 'market',
    width: 80,
    render: (row) => {
      const color = marketColors[row.market]
      const style = color
        ? `color: ${color.secondary}; background: ${color.primary}; font-weight: 600`
        : 'font-weight: 600'
      return h(NTag, { size: 'small', bordered: false, style }, { default: () => row.market })
    },
  },
  { title: '股票代碼', key: 'stockCode', width: 110 },
  {
    title: '類型',
    key: 'type',
    width: 90,
    render: (row) =>
      h(
        NTag,
        {
          size: 'small',
          type: row.type === 'Buy' ? 'error' : 'success',
          bordered: false,
        },
        { default: () => (row.type === 'Buy' ? '買入' : '賣出') },
      ),
  },
  { title: '價格', key: 'price', width: 100 },
  { title: '數量', key: 'quantity', width: 80 },
  {
    title: '操作',
    key: 'actions',
    width: 110,
    render: (row) =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () => [
            h(
              NButton,
              { size: 'small', quaternary: true, onClick: () => openEdit(row) },
              { icon: () => h(NIcon, null, { default: () => h(EditRound) }) },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDelete(row.id),
                positiveText: '確定',
                negativeText: '取消',
              },
              {
                trigger: () =>
                  h(
                    NButton,
                    { size: 'small', quaternary: true, type: 'error' },
                    { icon: () => h(NIcon, null, { default: () => h(DeleteRound) }) },
                  ),
                default: () => '確定要刪除這筆交易嗎？',
              },
            ),
          ],
        },
      ),
  },
]

const loadTransactions = async () => {
  isLoading.value = true

  try {
    const result = await getTransactions()

    if (!result.ok) {
      errorMessage.value = result.message
      return
    }

    transactions.value = result.data
  } catch {
    errorMessage.value = '網路連線發生問題，請稍後再試'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadTransactions)
</script>

<template>
  <!-- 頁首 -->
  <div class="mb-6 flex items-center justify-between">
    <n-h2 class="m-0!">
      <n-text type="primary">交易記錄</n-text>
    </n-h2>
    <n-button type="primary" @click="openCreate">
      <template #icon>
        <n-icon><AddRound /></n-icon>
      </template>
      新增交易
    </n-button>
  </div>

  <!-- 篩選列 -->
  <n-flex class="mb-4" :wrap="false">
    <n-select
      v-model:value="filterMarket"
      :options="marketOptions"
      placeholder="市場"
      class="w-36"
      clearable
    />
    <n-select
      v-model:value="filterType"
      :options="typeOptions"
      placeholder="類型"
      class="w-32"
      clearable
    />
    <n-input v-model:value="filterCode" placeholder="搜尋股票代碼" class="w-48" clearable>
      <template #prefix>
        <n-icon><SearchRound /></n-icon>
      </template>
    </n-input>
  </n-flex>

  <!-- 載入中 -->
  <div v-if="isLoading" class="flex justify-center py-20">
    <n-spin size="large" />
  </div>

  <template v-else>
    <!-- 錯誤提示 -->
    <n-alert v-if="errorMessage" type="error" :bordered="false" class="mb-4">{{
      errorMessage
    }}</n-alert>

    <!-- 表格資料 -->
    <n-data-table
      v-else-if="filterTransactions.length > 0"
      :columns="columns"
      :data="filterTransactions"
      :bordered="false"
      striped
    />

    <!-- 空資料 -->
    <div v-else-if="transactions.length === 0" class="flex justify-center py-20">
      <n-empty description="尚無交易記錄，點擊右上角新增第一筆吧！" />
    </div>

    <!-- 篩選後無資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="找不到符合條件的交易記錄" />
    </div>
  </template>

  <!-- 新增/編輯 Modal -->
  <TransactionModal
    v-model:show="showModal"
    :transaction="editingTransaction"
    @refresh="loadTransactions"
  />

  <!-- TODO: 增加交易熱點圖 -->
</template>
