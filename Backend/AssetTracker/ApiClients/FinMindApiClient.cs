using Mapster;
using Microsoft.AspNetCore.WebUtilities;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.FinMind.StockInfo;
using Project.Shared.DTOs.FinMind.StockPrice;
using Project.Shared.DTOs.Stock;
using Project.Shared.Types;
using System.Text.Json;
using System.Text.Json.Nodes;
using static AssetTracker.Common.StockConfig;

namespace AssetTracker.ApiClients
{
    public class FinMindApiClient(HttpClient httpClient, [FromKeyedServices("ApiResponse")] JsonSerializerOptions options, ILogger<FinMindApiClient> logger) : IStockApiClients
    {
        private record MarketDatasetMapping(string StockPrice, string StockData);

        private static MarketDatasetMapping? GetDataSet(StockMarketType market) => market switch
        {
            StockMarketType.TW => new("TaiwanStockPrice", "TaiwanStockInfo"),
            StockMarketType.US => new("USStockPrice", "USStockInfo"),
            StockMarketType.JP => new("JapanStockPrice", "JapanStockInfo"),
            _ => null
        };

        public async Task<Result<StockInfo>> GetStockInfoAsync(StockMarketType market, string code)
        {
            StockInfo? stockInfo;
            try
            {
                stockInfo = await FetchStockInfoAsync(market, code);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "呼叫 FinMind 股票資訊 API 失敗。market: {Market}, code: {Code}", market, code);
                return Result<StockInfo>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供");
            }

            if (stockInfo == null)
            {
                return Result<StockInfo>.Failure(ResultCode.BusinessRuleViolation, "不支援此檔股票");
            }

            return Result<StockInfo>.Success(stockInfo);
        }

        public async Task<Result<List<StockPriceHistory>>> GetStockPriceAsync(StockMarketType market, string code, DateTime startDate, DateTime endDate)
        {
            List<StockPriceHistory>? result = null;
            StockInfo? stockInfo = null;
            try
            {
                stockInfo = await FetchStockInfoAsync(market, code);

                if (stockInfo == null)
                {
                    return Result<List<StockPriceHistory>>.Failure(ResultCode.BusinessRuleViolation, "不支援此檔股票");
                }

                var stockPrice = await FetchStockPriceAsync(market, code, startDate, endDate);

                if (stockPrice.Count < 1)
                {
                    return Result<List<StockPriceHistory>>.Failure(ResultCode.BusinessRuleViolation, "查無此日的價格資訊");
                }

                result = stockPrice.Adapt<List<StockPriceHistory>>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "呼叫 FinMind 股票資訊 API 失敗。market: {Market}, code: {Code}", market, code);
                return Result<List<StockPriceHistory>>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供");
            }

            result.ForEach(x =>
            {
                x.Name = stockInfo.Name;
                x.Exchange = stockInfo.Exchange;
                x.StockMarket = market;
                x.Currency = market.GetCurrency() ?? throw new InvalidOperationException("Currency should not be null");
            });

            return Result<List<StockPriceHistory>>.Success(result);
        }

        /// <summary>
        /// 根據市場類型和股票代碼，從 FinMind API 取得股票名稱。
        /// </summary>
        /// <param name="market">股票市場類型（如台股、美股等）</param>
        /// <param name="code">股票代碼</param>
        /// <returns>API 回傳資料中第一筆的股票名稱；若查無資料則回傳 null</returns>
        private async Task<StockInfo?> FetchStockInfoAsync(StockMarketType market, string code)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["dataset"] = GetDataSet(market)?.StockData,
                ["data_id"] = code
            };

            var url = QueryHelpers.AddQueryString("data", queryParams);
            // 例外不在此處攔截，交由 public 方法統一轉成 ExternalApiError
            var response = await httpClient.GetFromJsonAsync<JsonNode>(url);

            return market switch
            {
                StockMarketType.TW => DeserializeStockInfo<TaiwanStockInfo>(response),
                StockMarketType.JP => DeserializeStockInfo<JapanStockInfo>(response),
                StockMarketType.US => DeserializeStockInfo<USStockInfo>(response),
                _ => null
            };
        }

        private async Task<List<StockPrice>> FetchStockPriceAsync(StockMarketType market, string code, DateTime startDate, DateTime endDate)
        {
            var queryParams = new Dictionary<string, string?>()
            {
                ["dataset"] = GetDataSet(market)?.StockPrice,
                ["data_id"] = code,
                ["start_date"] = startDate.ToString("yyyy-MM-dd"),
                ["end_date"] = endDate.ToString("yyyy-MM-dd"),
            };

            var url = QueryHelpers.AddQueryString("data", queryParams);
            var response = await httpClient.GetFromJsonAsync<JsonNode>(url);

            return market switch
            {
                StockMarketType.TW => DeserializeStockPrice<TaiwanStockPrice>(response),
                StockMarketType.JP => DeserializeStockPrice<JapanStockPrice>(response),
                StockMarketType.US => DeserializeStockPrice<USStockPrice>(response),
                _ => []
            };
        }

        private StockInfo? DeserializeStockInfo<T>(JsonNode? response) where T : class
        {
            var list = response?["data"].Deserialize<List<T>>(options) ?? [];
            return list.FirstOrDefault()?.Adapt<StockInfo>();
        }

        private List<StockPrice> DeserializeStockPrice<T>(JsonNode? response) where T : class
        {
            var list = response?["data"].Deserialize<List<T>>(options) ?? [];
            return list.Adapt<List<StockPrice>>();
        }
    }
}
