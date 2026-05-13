<script setup lang="ts">
import { computed, h, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import {
  NAlert,
  NButton,
  NCard,
  NCollapseTransition,
  NDataTable,
  NDivider,
  NEmpty,
  NFlex,
  NFloatButton,
  NH2,
  NIcon,
  NInput,
  NPopconfirm,
  NSelect,
  NSpace,
  NSpin,
  NTag,
  NText,
  useMessage,
} from 'naive-ui'
import type { DataTableColumns, SelectOption } from 'naive-ui'
import { breakpointsTailwind, useBreakpoints } from '@vueuse/core'

import {
  AddRound,
  DeleteRound,
  EditRound,
  KeyboardArrowDownRound,
  SearchRound,
} from '@vicons/material'

import MarketTag from '@/components/MarketTag.vue'
import TransactionModal from '@/components/TransactionModal.vue'
import type { TransactionResponse } from '@/types/transaction'
import { deleteTransaction, getTransactions } from '@/api/transaction'
import { transactionTypeColors } from '@/utils/colors'

// ---- Setup ----

const message = useMessage()
const route = useRoute()
const breakpoints = useBreakpoints(breakpointsTailwind)
const isMobile = breakpoints.smaller('md')

// ---- State ----

const transactions = ref<TransactionResponse[]>([])
const errorMessage = ref<string | null>(null)
const isLoading = ref(false)
const expandedId = ref<number>()

// ---- 篩選 ----

const filterMarket = ref<string | undefined>((route.query.stockMarket as string) ?? undefined)
const filterType = ref<string | undefined>(undefined)
const filterCode = ref((route.query.stockCode as string) ?? '')

const marketOptions: SelectOption[] = [
  { label: '台股 TW', value: 'TW' },
  { label: '美股 US', value: 'US' },
  { label: '日股 JP', value: 'JP' },
]

const typeOptions: SelectOption[] = [
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

// ---- Modal ----

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

// ---- Table columns（桌機版）----

const columns: DataTableColumns<TransactionResponse> = [
  { title: '日期', key: 'date', width: 110, render: (row) => row.date.slice(0, 10) },
  {
    title: '市場',
    key: 'market',
    width: 80,
    render: (row) => h(MarketTag, { market: row.market }),
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

// ---- Helpers ----

const toggleExpand = (id: number) => {
  expandedId.value = expandedId.value === id ? undefined : id
}

// ---- API ----

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

// ---- Lifecycle ----

onMounted(loadTransactions)
</script>

<template>
  <!-- 頁首 -->
  <div v-if="!isMobile" class="mb-6 flex items-center justify-between">
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
  <n-flex v-if="!isMobile" :wrap="false" class="mb-4 gap-2">
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

  <!-- 篩選列（手機版） -->
  <div v-else class="flex flex-col gap-2">
    <n-flex :wrap="false" class="gap-2">
      <n-select
        v-model:value="filterMarket"
        :options="marketOptions"
        placeholder="市場"
        class="flex-1"
        clearable
      />
      <n-select
        v-model:value="filterType"
        :options="typeOptions"
        placeholder="類型"
        class="flex-1"
        clearable
      />
    </n-flex>
    <n-input v-model:value="filterCode" placeholder="搜尋股票代碼" class="w-full" clearable>
      <template #prefix>
        <n-icon><SearchRound /></n-icon>
      </template>
    </n-input>
  </div>

  <!-- 篩選與內容分界（手機版） -->
  <n-divider v-if="isMobile" class="my-3!" />

  <!-- 載入中 -->
  <div v-if="isLoading" class="flex justify-center py-20">
    <n-spin size="large" />
  </div>

  <template v-else>
    <!-- 錯誤提示 -->
    <n-alert v-if="errorMessage" type="error" :bordered="false" class="mb-4">{{
      errorMessage
    }}</n-alert>

    <template v-else-if="filterTransactions.length > 0">
      <!-- 交易紀錄表格 -->
      <n-data-table
        v-if="!isMobile"
        :columns="columns"
        :data="filterTransactions"
        :bordered="false"
        striped
      />

      <!-- 手機版 -->
      <div v-else class="flex flex-col gap-3">
        <n-card
          v-for="transaction in filterTransactions"
          :key="transaction.id"
          size="medium"
          :bordered="false"
          content-style="padding: 0;"
          class="overflow-hidden shadow"
          @click="toggleExpand(transaction.id)"
        >
          <div class="flex">
            <!-- 左色條 -->
            <div
              class="w-1 shrink-0"
              :style="{ background: transactionTypeColors[transaction.type].primary }"
            />

            <div class="flex-1 p-3">
              <!-- 卡片頭 -->
              <div class="mb-2 flex items-center gap-2">
                <MarketTag :market="transaction.market" />
                <n-text class="text-base font-semibold">{{ transaction.stockCode }}</n-text>
                <n-tag
                  size="small"
                  :bordered="false"
                  :type="transaction.type === 'Buy' ? 'error' : 'success'"
                  class="ml-auto"
                >
                  {{ transaction.type === 'Buy' ? '買入' : '賣出' }}
                </n-tag>
                <n-icon
                  size="20"
                  class="text-gray-400 transition-transform duration-200"
                  :class="{ 'rotate-180': expandedId === transaction.id }"
                >
                  <KeyboardArrowDownRound />
                </n-icon>
              </div>

              <!-- 卡片內容 -->
              <div class="flex items-center justify-between gap-2 text-sm">
                <n-text depth="3" class="shrink-0">{{ transaction.date.slice(0, 10) }}</n-text>
                <n-text class="min-w-0 text-right">
                  <span class="text-xs opacity-70">
                    {{ transaction.quantity }} 股 × ${{ transaction.price }} =
                  </span>
                  <span class="ml-1 font-semibold">
                    ${{ (transaction.quantity * transaction.price).toLocaleString() }}
                  </span>
                </n-text>
              </div>

              <!-- 展開的操作區 -->
              <n-collapse-transition :show="expandedId === transaction.id">
                <n-divider class="my-2!" />
                <div class="flex justify-end gap-2" @click.stop>
                  <n-button size="small" secondary @click="openEdit(transaction)">
                    <template #icon>
                      <n-icon><EditRound /></n-icon>
                    </template>
                    編輯
                  </n-button>
                  <n-popconfirm
                    :positive-text="'確定'"
                    :negative-text="'取消'"
                    @positive-click="handleDelete(transaction.id)"
                  >
                    <template #trigger>
                      <n-button size="small" secondary type="error">
                        <template #icon>
                          <n-icon><DeleteRound /></n-icon>
                        </template>
                        刪除
                      </n-button>
                    </template>
                    確定要刪除這筆交易嗎？
                  </n-popconfirm>
                </div>
              </n-collapse-transition>
            </div>
          </div>
        </n-card>
      </div>
    </template>

    <!-- 空資料 -->
    <div v-else-if="transactions.length === 0" class="flex justify-center py-20">
      <n-empty description="尚無交易記錄，點擊新增按鈕建立第一筆紀錄吧！" />
    </div>

    <!-- 篩選後無資料 -->
    <div v-else class="flex justify-center py-20">
      <n-empty description="找不到符合條件的交易記錄" />
    </div>
  </template>

  <!-- 手機版浮動新增按鈕 -->
  <n-float-button v-if="isMobile" :right="16" :bottom="80" type="primary" @click="openCreate">
    <n-icon><AddRound /></n-icon>
  </n-float-button>

  <!-- 新增/編輯 Modal -->
  <TransactionModal
    v-model:show="showModal"
    :transaction="editingTransaction"
    @refresh="loadTransactions"
  />
</template>
