# WIP: 持倉總覽加入未實現損益欄位

> 最後更新：2026-04-24（回家繼續）

## 目標

在持倉總覽（PositionsView）表格加入 4 個新欄位：
1. 公司名稱（獨立欄）
2. 現價
3. 未實現損益（顯示幣別換算後）
4. 報酬率

## 目前進度

### 已完成
- 後端批次股價端點 `POST /api/stock/latest-prices` 串接
- 新增 `src/api/stock.ts` 和 `src/types/stock.ts`
- `PositionsView.vue` `onMounted` 已經呼叫批次股價 API 拿到資料
- `EnrichedPosition` 擴充了 `stockName`、`currentPrice`、`unrealizedPnl`、`unrealizedPnlRate` 四個欄位

### 進行中：資料流重構（最重要）

**決策記錄**：`C:\Users\Enzo\.claude\projects\C--EnzoCode-interview-Project\memory\project_positions_view_refactor.md`

**核心想法**：分離資料層（原幣）與顯示層（換算後），用 `computed` 取代 `handleCurrencyChange` 裡的 `forEach` 手動 sync。

**原因**：原本 `convertedTotalCost` 存在 `EnrichedPosition` 上，切幣別要 `forEach` 重算。現在要加「未實現損益換算」，如果照原 pattern 會變成切幣別要 forEach 重算兩個欄位，將來每加一個換算欄位就要多一行 forEach，越來越亂。

**改法**：
- `EnrichedPosition` 只存原幣/原始資料（`averagePrice`, `quantity`, `stockName`, `currentPrice`, `unrealizedPnl` 原幣, `unrealizedPnlRate` 原比率）
- 不存 `convertedTotalCost`、不存 `convertedUnrealizedPnl`
- View 加 `conversionRates` ref + `displayedPositions` computed
- `displayedPositions` 在 computed 裡算出 `totalCost`、`convertedTotalCost`、`convertedUnrealizedPnl`
- `handleCurrencyChange` 只做「更新 `conversionRates.value`」一件事，不再 forEach
- 子組件 (`useMarketChart`, `usePositionColumns`) 收 `displayedPositions`，內部讀 `row.convertedTotalCost` 的地方不用動

## 接下來要做的事（依序）

### 1. 型別分層

檔案：`src/types/Position.ts`

- `EnrichedPosition` 只留「原幣/原始」欄位（stockName, currentPrice, unrealizedPnl, unrealizedPnlRate）。**不要加 `convertedTotalCost`、`convertedUnrealizedPnl`**
- 新增 `DisplayedPosition` 介面，多 `totalCost`、`convertedTotalCost`、`convertedUnrealizedPnl?`

### 2. View 資料流重構

檔案：`src/views/positions/PositionsView.vue`

- 加 `conversionRates` ref：型別 `Record<CurrencyType, number>`，初始值空物件或從 exchangeRate 拿
- 把現在的 `enrichedPosition` computed 改名成 `displayedPositions`，型別改為 `DisplayedPosition[]`，加上換算邏輯：
  - `totalCost = averagePrice * quantity`
  - `convertedTotalCost = totalCost / conversionRates.value[marketCurrencyMap[stockMarket]]`
  - `convertedUnrealizedPnl = unrealizedPnl / ...`（unrealizedPnl 為 undefined 時也是 undefined）
- `handleCurrencyChange` 改寫：只做 `conversionRates.value = result.data.conversionRates`，**不再 forEach**
- `onMounted` 初始化時：`conversionRates.value` 直接塞從 `getExchangeRate` 拿到的 rates

### 3. 子組件改收 `displayedPositions`

- `useMarketChart(displayedPositions, stockChartRef)` — 內部 `pos.convertedTotalCost` 的讀法不用改
- `useStockChart(displayedPositions)` — 同上
- `usePositionColumns` — 同上，且要新增 4 個欄位的 render

### 4. `usePositionColumns` 加新欄位

找我（Claude）幫你設計欄位顯示，紅綠配色、`NTag`、`renderCurrencyHintTitle` 等。

決定的設計：
- 公司名稱獨立欄
- 未實現損益**也做換算欄**（顯示幣別）

### 5. 驗證

- 切換幣別時，持倉表格和圓餅圖都要正確更新
- 股價查詢失敗的股票（`failed` 陣列）表格對應欄位顯示 `-` 或空白
- 圓餅圖的總額計算仍正確

## 檔案清單

改動的檔案：
- `src/types/Position.ts`
- `src/types/stock.ts`（新增）
- `src/api/stock.ts`（新增）
- `src/views/positions/PositionsView.vue`
- `src/views/positions/usePositionColumns.ts`
- `src/views/positions/useMarketChart.ts`（簽章換型別）
- `src/views/positions/useStockChart.ts`（簽章換型別）
