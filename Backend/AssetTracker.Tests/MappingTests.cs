using Mapster;
using Project.Data.Model;
using Project.Shared.DTOs.Auth;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.DTOs.ExchangeRate.ExchangeRateAPI;
using Project.Shared.DTOs.FinMind.StockInfo;
using Project.Shared.DTOs.FinMind.StockPrice;
using Project.Shared.DTOs.Stock;
using Project.Shared.DTOs.Transaction;
using Project.Shared.Types;

namespace AssetTracker.Tests
{
    /// <summary>
    /// Mapster 設定 Fixture，確保整個測試類別只初始化一次
    /// </summary>
    public class MapsterFixture
    {
        public MapsterFixture()
        {
            MapsterConfig.SettingGlobalConfig();
        }
    }

    /// <summary>
    /// Mapster 映射配置測試
    /// 驗證所有 DTO 與 Entity 之間的映射是否正確
    /// </summary>
    public class MappingTest(MapsterFixture fixture) : IClassFixture<MapsterFixture>
    {
        private readonly MapsterFixture _fixture = fixture;

        #region 身份驗證相關映射

        /// <summary>
        /// 測試註冊請求轉換為使用者實體
        /// 驗證：帳號和名稱應正確映射，密碼因欄位名稱不同不應被映射
        /// </summary>
        [Fact(DisplayName = "註冊請求→使用者：帳號名稱正確映射，密碼不映射")]
        public void RegisterRequest_To_User_ShouldMapCorrectly()
        {
            // Arrange - 準備測試資料
            var request = new RegisterRequest
            {
                Account = "testuser",
                Password = "testpassword123",
                Name = "Test User"
            };

            // Act - 執行映射
            var user = request.Adapt<User>();

            // Assert - 驗證結果
            Assert.Equal(request.Account, user.Account);
            Assert.Equal(request.Name, user.Name);

            // Password 不應該被映射（來源是 Password，目標是 PasswordHash，名稱不同）
            Assert.Equal(string.Empty, user.PasswordHash);
        }

        #endregion

        #region 股價轉換為資料庫實體

        /// <summary>
        /// 測試統一股價格式轉換為股價歷史實體
        /// 驗證：欄位名稱對應（如 Open→OpeningPrice, Max→High）
        /// 驗證：被 Ignore 的欄位應維持預設值
        /// </summary>
        [Fact(DisplayName = "統一股價→股價歷史：欄位名稱正確對應")]
        public void StockPrice_To_StockPriceHistory_ShouldMapCorrectly()
        {
            // Arrange - 準備台積電股價測試資料
            var stockPrice = new StockPrice
            {
                Date = new DateTime(2024, 1, 15),
                StockId = "2330",
                TradingVolume = 50000000,
                Open = 580.00m,
                Max = 585.00m,
                Min = 575.00m,
                Close = 582.00m
            };

            // Act - 執行映射
            var history = stockPrice.Adapt<StockPriceHistory>();

            // Assert - 驗證欄位映射
            Assert.Equal(stockPrice.Date, history.Date);
            Assert.Equal(stockPrice.StockId, history.Code);          // StockId → Code
            Assert.Equal(stockPrice.TradingVolume, history.Volume);  // TradingVolume → Volume
            Assert.Equal(stockPrice.Open, history.OpeningPrice);     // Open → OpeningPrice
            Assert.Equal(stockPrice.Max, history.High);              // Max → High
            Assert.Equal(stockPrice.Min, history.Low);               // Min → Low
            Assert.Equal(stockPrice.Close, history.ClosingPrice);    // Close → ClosingPrice

            // 被 Ignore 的欄位應維持預設值（這些欄位會在其他地方設定）
            Assert.Equal(0, history.Id);
            Assert.Equal(string.Empty, history.Exchange);
            Assert.Equal(string.Empty, history.Name);
        }

        #endregion

        #region 匯率 API 回應轉換為匯率回應 DTO

        /// <summary>
        /// 測試 Pair API 回應轉換為匯率回應 DTO
        /// 驗證：單一匯率應包裝為 Dictionary，BaseCode/TargetCode 正確解析為 CurrencyType
        /// 驗證：Unix 時間戳應正確轉換為 DateTime（只取日期部分）
        /// </summary>
        [Fact(DisplayName = "Pair匯率回應→匯率DTO：單一匯率包裝為Dictionary")]
        public void PairResponse_To_ExchangeRateResponse_ShouldMapCorrectly()
        {
            // Arrange - 準備匯率 API 回應資料
            // Unix 時間戳 1705276800 對應 2024-01-15 00:00:00 UTC
            var unixTimestamp = 1705276800L;
            var pairResponse = new PairResponse
            {
                BaseCode = "TWD",
                TargetCode = "USD",
                ConversionRate = 0.032m,
                TimeLastUpdateUnix = unixTimestamp
            };

            // Act - 執行映射
            var response = pairResponse.Adapt<ExchangeRateResponse>();

            // Assert - 驗證欄位映射
            Assert.Equal(CurrencyType.TWD, response.Currency);

            // 單一匯率應包裝為只有一筆的 Dictionary
            Assert.Single(response.ConversionRates);
            Assert.Equal(0.032m, response.ConversionRates[CurrencyType.USD]);

            // TimeLastUpdateUnix 透過 DateTimeOffset.FromUnixTimeSeconds 轉換後取日期
            var expectedDate = new DateTime(2024, 1, 15);
            Assert.Equal(expectedDate, response.Date);
        }

        /// <summary>
        /// 測試 Standard API 回應轉換為匯率回應 DTO
        /// 驗證：只保留有效的 CurrencyType，過濾掉不支援的幣別
        /// </summary>
        [Fact(DisplayName = "Standard匯率回應→匯率DTO：過濾有效幣別")]
        public void StandardResponse_To_ExchangeRateResponse_ShouldMapCorrectly()
        {
            // Arrange - 準備匯率 API 回應資料
            var unixTimestamp = 1705276800L;
            var standardResponse = new StandardResponse
            {
                BaseCode = "USD",
                ConversionRates = new Dictionary<string, decimal>
                {
                    { "TWD", 31.5m },
                    { "JPY", 148.2m },
                    { "USD", 1.0m },
                    { "EUR", 0.92m },   // 不在 CurrencyType 中，應被過濾
                    { "GBP", 0.79m }    // 不在 CurrencyType 中，應被過濾
                },
                TimeLastUpdateUnix = unixTimestamp
            };

            // Act - 執行映射
            var response = standardResponse.Adapt<ExchangeRateResponse>();

            // Assert - 驗證欄位映射
            Assert.Equal(CurrencyType.USD, response.Currency);

            // 只應保留 TWD、JPY、USD 三種有效幣別
            Assert.Equal(3, response.ConversionRates.Count);
            Assert.Equal(31.5m, response.ConversionRates[CurrencyType.TWD]);
            Assert.Equal(148.2m, response.ConversionRates[CurrencyType.JPY]);
            Assert.Equal(1.0m, response.ConversionRates[CurrencyType.USD]);

            // 不支援的幣別不應存在
            Assert.DoesNotContain(response.ConversionRates, kv => kv.Key.ToString() == "EUR");

            var expectedDate = new DateTime(2024, 1, 15);
            Assert.Equal(expectedDate, response.Date);
        }

        #endregion

        #region 各市場股票資訊轉換為統一格式

        /// <summary>
        /// 測試台股股票資訊轉換為統一格式
        /// 映射規則：StockId→Code, StockName→Name, IndustryCategory→IndustryCategory, Type→Exchange
        /// </summary>
        [Fact(DisplayName = "台股資訊→統一格式：Type映射為Exchange")]
        public void TaiwanStockInfo_To_StockInfo_ShouldMapCorrectly()
        {
            // Arrange - 準備台積電股票資訊
            var taiwanStock = new TaiwanStockInfo
            {
                StockId = "2330",
                StockName = "台積電",
                IndustryCategory = "半導體業",
                Type = "twse",  // 台灣證券交易所
                Date = new DateTime(2024, 1, 15)
            };

            // Act - 執行映射
            var stockInfo = taiwanStock.Adapt<StockInfo>();

            // Assert - 驗證欄位映射
            Assert.Equal(taiwanStock.StockId, stockInfo.Code);
            Assert.Equal(taiwanStock.StockName, stockInfo.Name);
            Assert.Equal(taiwanStock.IndustryCategory, stockInfo.IndustryCategory);
            Assert.Equal(taiwanStock.Type, stockInfo.Exchange);  // 台股用 Type 作為交易所
        }

        /// <summary>
        /// 測試日股股票資訊轉換為統一格式
        /// 映射規則：StockId→Code, StockName→Name, Sector→IndustryCategory, Exchange→Exchange
        /// </summary>
        [Fact(DisplayName = "日股資訊→統一格式：Sector映射為IndustryCategory")]
        public void JapanStockInfo_To_StockInfo_ShouldMapCorrectly()
        {
            // Arrange - 準備豐田汽車股票資訊
            var japanStock = new JapanStockInfo
            {
                StockId = "7203",
                StockName = "トヨタ自動車",
                Sector = "輸送用機器",  // 日股用 Sector 表示產業
                Exchange = "TSE",        // 東京證券交易所
                Date = new DateTime(2024, 1, 15)
            };

            // Act - 執行映射
            var stockInfo = japanStock.Adapt<StockInfo>();

            // Assert - 驗證欄位映射
            Assert.Equal(japanStock.StockId, stockInfo.Code);
            Assert.Equal(japanStock.StockName, stockInfo.Name);
            Assert.Equal(japanStock.Sector, stockInfo.IndustryCategory);  // Sector → IndustryCategory
            Assert.Equal(japanStock.Exchange, stockInfo.Exchange);
        }

        /// <summary>
        /// 測試美股股票資訊轉換為統一格式
        /// 映射規則：StockId→Code, StockName→Name, Subsector→IndustryCategory, Exchange 固定為空字串
        /// </summary>
        [Fact(DisplayName = "美股資訊→統一格式：Subsector映射為IndustryCategory")]
        public void USStockInfo_To_StockInfo_ShouldMapCorrectly()
        {
            // Arrange - 準備蘋果公司股票資訊
            var usStock = new USStockInfo
            {
                StockId = "AAPL",
                StockName = "Apple Inc.",
                Subsector = "Technology",  // 美股用 Subsector 表示產業
                Country = "USA",
                IPOYear = "1980",
                MarketCap = "3000000000000"
            };

            // Act - 執行映射
            var stockInfo = usStock.Adapt<StockInfo>();

            // Assert - 驗證欄位映射
            Assert.Equal(usStock.StockId, stockInfo.Code);
            Assert.Equal(usStock.StockName, stockInfo.Name);
            Assert.Equal(usStock.Subsector, stockInfo.IndustryCategory);  // Subsector → IndustryCategory
            Assert.Equal(string.Empty, stockInfo.Exchange);  // 美股 API 無交易所資訊，映射為空字串
        }

        #endregion

        #region 各市場股價轉換為統一格式

        /// <summary>
        /// 測試台股股價轉換為統一格式
        /// 台股特有欄位：TradingMoney（成交金額）、Spread（漲跌幅）、TradingTurnover（成交筆數）不會被映射
        /// </summary>
        [Fact(DisplayName = "台股股價→統一格式：Max/Min正確映射")]
        public void TaiwanStockPrice_To_StockPrice_ShouldMapCorrectly()
        {
            // Arrange - 準備台積電股價資料
            var taiwanPrice = new TaiwanStockPrice
            {
                Date = new DateTime(2024, 1, 15),
                StockId = "2330",
                TradingVolume = 50000000,      // 成交股數
                TradingMoney = 29000000000m,   // 成交金額（不會被映射）
                Open = 580.00m,
                Max = 585.00m,                 // 台股用 Max 表示最高價
                Min = 575.00m,                 // 台股用 Min 表示最低價
                Close = 582.00m,
                Spread = 2.0f,                 // 漲跌幅（不會被映射）
                TradingTurnover = 100000       // 成交筆數（不會被映射）
            };

            // Act - 執行映射
            var stockPrice = taiwanPrice.Adapt<StockPrice>();

            // Assert - 驗證欄位映射
            Assert.Equal(taiwanPrice.Date, stockPrice.Date);
            Assert.Equal(taiwanPrice.StockId, stockPrice.StockId);
            Assert.Equal(taiwanPrice.TradingVolume, stockPrice.TradingVolume);
            Assert.Equal(taiwanPrice.Open, stockPrice.Open);
            Assert.Equal(taiwanPrice.Max, stockPrice.Max);
            Assert.Equal(taiwanPrice.Min, stockPrice.Min);
            Assert.Equal(taiwanPrice.Close, stockPrice.Close);
        }

        /// <summary>
        /// 測試日股股價轉換為統一格式
        /// 欄位名稱差異：Volume→TradingVolume, High→Max, Low→Min
        /// </summary>
        [Fact(DisplayName = "日股股價→統一格式：High/Low映射為Max/Min")]
        public void JapanStockPrice_To_StockPrice_ShouldMapCorrectly()
        {
            // Arrange - 準備豐田汽車股價資料
            var japanPrice = new JapanStockPrice
            {
                Date = new DateTime(2024, 1, 15),
                StockId = "7203",
                Volume = 5000000,       // 日股用 Volume
                Open = 2500.00m,
                High = 2550.00m,        // 日股用 High 表示最高價
                Low = 2480.00m,         // 日股用 Low 表示最低價
                Close = 2530.00m
            };

            // Act - 執行映射
            var stockPrice = japanPrice.Adapt<StockPrice>();

            // Assert - 驗證欄位映射
            Assert.Equal(japanPrice.Date, stockPrice.Date);
            Assert.Equal(japanPrice.StockId, stockPrice.StockId);
            Assert.Equal(japanPrice.Volume, stockPrice.TradingVolume);  // Volume → TradingVolume
            Assert.Equal(japanPrice.Open, stockPrice.Open);
            Assert.Equal(japanPrice.High, stockPrice.Max);              // High → Max
            Assert.Equal(japanPrice.Low, stockPrice.Min);               // Low → Min
            Assert.Equal(japanPrice.Close, stockPrice.Close);
        }

        /// <summary>
        /// 測試美股股價轉換為統一格式
        /// 欄位名稱差異：Volume→TradingVolume, High→Max, Low→Min
        /// </summary>
        [Fact(DisplayName = "美股股價→統一格式：Volume映射為TradingVolume")]
        public void USStockPrice_To_StockPrice_ShouldMapCorrectly()
        {
            // Arrange - 準備蘋果公司股價資料
            var usPrice = new USStockPrice
            {
                Date = new DateTime(2024, 1, 15),
                StockId = "AAPL",
                Volume = 80000000,      // 美股用 Volume
                Open = 185.50m,
                High = 187.00m,         // 美股用 High 表示最高價
                Low = 184.00m,          // 美股用 Low 表示最低價
                Close = 186.50m
            };

            // Act - 執行映射
            var stockPrice = usPrice.Adapt<StockPrice>();

            // Assert - 驗證欄位映射
            Assert.Equal(usPrice.Date, stockPrice.Date);
            Assert.Equal(usPrice.StockId, stockPrice.StockId);
            Assert.Equal(usPrice.Volume, stockPrice.TradingVolume);  // Volume → TradingVolume
            Assert.Equal(usPrice.Open, stockPrice.Open);
            Assert.Equal(usPrice.High, stockPrice.Max);              // High → Max
            Assert.Equal(usPrice.Low, stockPrice.Min);               // Low → Min
            Assert.Equal(usPrice.Close, stockPrice.Close);
        }

        #endregion

        #region 交易實體轉換為回應 DTO

        /// <summary>
        /// 測試交易實體轉換為 API 回應格式
        /// 注意：StockMarket 映射到 Market（名稱不同）
        /// 注意：UserId、UpdatedAt、DeletedAt 不會被映射（回應不需要這些欄位）
        /// </summary>
        [Fact(DisplayName = "交易實體→回應DTO：StockMarket映射為Market")]
        public void Transaction_To_TransactionResponse_ShouldMapCorrectly()
        {
            // Arrange - 準備交易資料
            var transaction = new Transaction
            {
                Id = 1,
                UserId = 100,                          // 不會被映射到回應
                StockMarket = StockMarketType.TW,
                StockCode = "2330",
                Date = new DateTime(2024, 1, 15),
                Type = TransactionType.Buy,
                Quantity = 1000,
                Price = 580.00m,
                Remark = "定期定額",
                CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
                UpdatedAt = null,                      // 不會被映射到回應
                DeletedAt = null                       // 不會被映射到回應
            };

            // Act - 執行映射
            var response = transaction.Adapt<TransactionResponse>();

            // Assert - 驗證欄位映射
            Assert.Equal(transaction.Id, response.Id);
            Assert.Equal(transaction.StockCode, response.StockCode);
            Assert.Equal(transaction.StockMarket, response.Market);  // StockMarket → Market
            Assert.Equal(transaction.Date, response.Date);
            Assert.Equal(transaction.Type, response.Type);
            Assert.Equal(transaction.Price, response.Price);
            Assert.Equal(transaction.Quantity, response.Quantity);
            Assert.Equal(transaction.Remark, response.Remark);
            Assert.Equal(transaction.CreatedAt, response.CreatedAt);
        }

        #endregion
    }
}
