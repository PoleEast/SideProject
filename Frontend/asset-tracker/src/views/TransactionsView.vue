<script setup lang="ts">
import { computed, h, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import {
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
import TableSkeleton from '@/components/TableSkeleton.vue'
import CardListSkeleton from '@/components/CardListSkeleton.vue'
import type { TransactionResponse } from '@/types/transaction'
import { deleteTransaction, getTransactions } from '@/api/transaction'
import { transactionTypeColors } from '@/utils/colors'
import { useNetworkLoadingBar } from '@/composables/useNetworkLoadingBar'
import { useApiToast } from '@/composables/useApiToast'

// ---- Setup ----

const message = useMessage()
const route = useRoute()
useNetworkLoadingBar()
const breakpoints = useBreakpoints(breakpointsTailwind)
const isMobile = breakpoints.smaller('md')
const { handle } = useApiToast()

// ---- State ----

const transactions = ref<TransactionResponse[]>([])
const expandedId = ref<number>()
const isInitialLoading = ref<boolean>(true)
const isRefreshing = ref<boolean>(false)
const isInitError = ref<boolean>(false)
const deletingId = ref<number>()

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
                    {
                      size: 'small',
                      quaternary: true,
                      type: 'error',
                      loading: deletingId.value === row.id,
                    },
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
  isRefreshing.value = true
  isInitError.value = false
  const result = await handle(getTransactions())
  if (!result.ok) {
    isInitError.value = true
  } else {
    transactions.value = result.data
  }
  isRefreshing.value = false
}

const handleDelete = async (id: number) => {
  deletingId.value = id
  try {
    const result = await handle(deleteTransaction(id))
    if (!result.ok) return
    message.success('已刪除交易')
    await loadTransactions()
  } finally {
    deletingId.value = undefined
  }
}

// ---- Lifecycle ----

onMounted(async () => {
  await loadTransactions()
  isInitialLoading.value = false
})
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
  <div v-else class="mb-1 flex flex-col gap-2">
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

  <!-- 初次載入：顯示 Skeleton -->
  <template v-if="isInitialLoading">
    <n-card v-if="!isMobile" title="交易明細" size="small" bordered>
      <TableSkeleton :rows="8" />
    </n-card>
    <CardListSkeleton v-else :count="6" />
  </template>

  <template v-else>
    <!-- 載入失敗 -->
    <div v-if="isInitError" class="flex justify-center py-20">
      <n-empty description="無法載入交易記錄" size="large">
        <template #extra>
          <n-button type="primary" @click="loadTransactions">重試</n-button>
        </template>
      </n-empty>
    </div>

    <template v-else-if="filterTransactions.length > 0">
      <n-card v-if="!isMobile" title="交易明細" size="small" bordered>
        <!-- 交易紀錄表格 -->
        <n-data-table
          :columns="columns"
          :data="filterTransactions"
          :loading="isRefreshing"
          :bordered="false"
          :scroll-x="700"
          striped
        />
      </n-card>

      <!-- 手機版 -->
      <n-spin v-else :show="isRefreshing">
        <div class="flex flex-col gap-3">
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
                        <n-button
                          size="small"
                          secondary
                          type="error"
                          :loading="deletingId === transaction.id"
                        >
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
      </n-spin>
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
