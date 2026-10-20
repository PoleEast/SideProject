<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { CircleAlert, X } from '@lucide/vue'
import type { AcceptableValue } from 'reka-ui'
import { toast } from 'vue-sonner'

import { useRegle } from '@regle/core'
import { maxLength, required, withMessage } from '@regle/rules'

import { createGroup } from '@/api/group'
import IconTile from '@/components/IconTile.vue'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import { groupIcons } from '@/lib/groupIcon'
import { currencyLabels } from '@/lib/money'
import type { CurrencyType } from '@/types/currency'
import type { GroupIconType } from '@/types/group'

interface CreateGroupFormData {
  name: string
  description: string
  icon: GroupIconType | null
  baseCurrency: CurrencyType
}

const open = defineModel<boolean>('open', { required: true })

const router = useRouter()

const formData = ref<CreateGroupFormData>({
  name: '',
  description: '',
  icon: null,
  baseCurrency: 'TWD',
})
const isSubmitting = ref(false)
const errorMessage = ref('')

const selectedIcon = computed(() => (formData.value.icon ? groupIcons[formData.value.icon] : null))

const { r$ } = useRegle(
  formData,
  {
    name: {
      required: withMessage(required, '請輸入群組名稱'),
      maxLength: withMessage(maxLength(32), '名稱最多32個字'),
    },
    description: {
      maxLength: withMessage(maxLength(200), '描述最多200個字'),
    },
    icon: {
      required: withMessage(required, '請選一個圖示'),
    },
    baseCurrency: {
      required: withMessage(required, '請選結算幣別'),
    },
  },
  { autoDirty: false },
)

const handleIconChange = (value: AcceptableValue | AcceptableValue[]) => {
  if (typeof value !== 'string') {
    return
  }
  formData.value.icon = value as GroupIconType
}

const handleCurrencyChange = (value: AcceptableValue | AcceptableValue[]) => {
  if (typeof value !== 'string') {
    return
  }
  formData.value.baseCurrency = value as CurrencyType
}

const handleOpenChange = (value: boolean) => {
  if (isSubmitting.value) {
    return
  }
  open.value = value
}

const handleSubmit = async () => {
  const { valid, data } = await r$.$validate()
  if (!valid) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''
  const result = await createGroup({
    name: data.name.trim(),
    description: data.description?.trim() ?? '',
    baseCurrency: data.baseCurrency,
    icon: data.icon,
  })
  isSubmitting.value = false

  if (!result.ok) {
    errorMessage.value = result.message
    return
  }

  toast.success(`${result.data.name} 建立成功`, { description: '接著邀請朋友加入吧' })
  router.push({ name: 'group', params: { groupId: result.data.id } })
}

watch(open, (isOpen) => {
  if (!isOpen) {
    return
  }
  r$.$reset({ toOriginalState: true })
  errorMessage.value = ''
})
</script>

<template>
  <Drawer :open="open" @update:open="handleOpenChange">
    <DrawerContent>
      <DrawerHeader class="flex-row items-start gap-2">
        <div class="flex flex-1 flex-col gap-0.5 pt-1.5">
          <DrawerTitle>建立群組</DrawerTitle>
          <DrawerDescription>建好之後再邀請朋友加入</DrawerDescription>
        </div>
        <DrawerClose as-child>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            class="text-muted-foreground size-11 rounded-full"
            aria-label="關閉"
            :disabled="isSubmitting"
          >
            <X class="size-5.5" aria-hidden="true" />
          </Button>
        </DrawerClose>
      </DrawerHeader>
      <form class="flex min-h-0 flex-1 flex-col" @submit.prevent="handleSubmit">
        <div class="min-h-0 flex-1 overflow-y-auto p-5">
          <FieldGroup>
            <Field :data-invalid="r$.name.$error">
              <FieldLabel for="group-name">名稱</FieldLabel>
              <div class="flex gap-2">
                <IconTile
                  v-if="selectedIcon"
                  size="lg"
                  :icon="selectedIcon.icon"
                  :color="selectedIcon.color"
                  :label="selectedIcon.label"
                  aria-hidden="true"
                />
                <span
                  v-else
                  class="border-input size-12 shrink-0 rounded-md border-2 border-dashed"
                  aria-hidden="true"
                />
                <Input
                  id="group-name"
                  v-model="formData.name"
                  type="text"
                  :aria-invalid="r$.name.$error"
                  @blur="r$.name.$touch()"
                  @focus="r$.name.$reset()"
                  autocomplete="off"
                  placeholder="沖繩旅行"
                />
              </div>
              <FieldError :errors="r$.name.$errors" />
            </Field>
            <FieldSet>
              <FieldLegend variant="label">圖示</FieldLegend>
              <ToggleGroup
                type="single"
                variant="chip"
                size="chip"
                :spacing="2"
                class="grid w-full grid-cols-4"
                :model-value="formData.icon"
                @update:model-value="handleIconChange"
              >
                <ToggleGroupItem
                  v-for="(option, iconType) in groupIcons"
                  :key="iconType"
                  :value="iconType"
                >
                  <IconTile
                    size="sm"
                    :icon="option.icon"
                    :color="option.color"
                    :label="option.label"
                    aria-hidden="true"
                  />
                  {{ option.label }}
                </ToggleGroupItem>
              </ToggleGroup>
              <FieldError :errors="r$.icon.$errors" />
            </FieldSet>
            <Field :data-invalid="r$.description.$error">
              <FieldLabel for="group-description" class="gap-0">
                描述<span class="text-muted-foreground font-normal">（選填）</span>
              </FieldLabel>
              <Input
                id="group-description"
                v-model="formData.description"
                type="text"
                :aria-invalid="r$.description.$error"
                @blur="r$.description.$touch()"
                @focus="r$.description.$reset()"
                autocomplete="off"
                placeholder="3月出發，四天三夜"
              />
              <FieldError :errors="r$.description.$errors" />
            </Field>
            <FieldSet>
              <FieldLegend variant="label">基準幣</FieldLegend>
              <ToggleGroup
                type="single"
                variant="chip"
                size="chip"
                :spacing="2"
                class="grid w-full grid-cols-3"
                :model-value="formData.baseCurrency"
                @update:model-value="handleCurrencyChange"
              >
                <ToggleGroupItem
                  v-for="(label, currency) in currencyLabels"
                  :key="currency"
                  :value="currency"
                >
                  <span class="font-semibold">{{ currency }}</span>
                  <span class="text-muted-foreground">{{ label }}</span>
                </ToggleGroupItem>
              </ToggleGroup>
              <FieldDescription>結算和還款都用這個幣別，記了第一筆帳就無法更改</FieldDescription>
            </FieldSet>
          </FieldGroup>
        </div>
        <DrawerFooter>
          <Alert v-if="errorMessage" variant="destructive">
            <CircleAlert />
            <AlertDescription>{{ errorMessage }}</AlertDescription>
          </Alert>
          <Button type="submit" size="lg" :disabled="isSubmitting">
            <Spinner v-if="isSubmitting" />
            {{ isSubmitting ? '建立中…' : '建立群組' }}
          </Button>
        </DrawerFooter>
      </form>
    </DrawerContent>
  </Drawer>
</template>
