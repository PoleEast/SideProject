using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Api.ApiClients;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Stock;
using Project.Shared.Types;

namespace Project.Api.Services
{
    public class StockService(IStockApiClients stockApiClients, ApplicationDbContext dbContext, ILogger<StockService> logger)
    {
        public async Task<Result<StockPriceResponse>> GetLatestStockPriceAsync(StockMarketType market, string code, DateOnly? asOf)
        {
            var batchResult = await GetLatestStockPricesAsync(
                [
                    new() { StockMarket = market, Code = code }
                ],
                asOf);

            if (!batchResult.IsSuccess || batchResult.Value == null)
            {
                return Result<StockPriceResponse>.Failure(batchResult.Code, batchResult.Message);
            }

            if (batchResult.Value.Failed.Count > 0)
            {
                return Result<StockPriceResponse>.Failure(
                    ResultCode.BusinessRuleViolation, batchResult.Value.Failed[0].Message);
            }

            if (batchResult.Value.Succeeded.Count == 0)
            {
                return Result<StockPriceResponse>.Failure(ResultCode.NotFound, "查無該期間的股價資料");
            }

            return Result<StockPriceResponse>.Success(batchResult.Value.Succeeded[0]);
        }

        public async Task<Result<BatchStockInfoResponse>> GetStockInfosAsync(List<StockIdentifier> requests)
        {
            var response = new BatchStockInfoResponse();

            var distinctRequests = requests
                .GroupBy(r => (r.StockMarket, Code: r.Code.Trim().ToUpperInvariant()))
                .Select(g => new StockIdentifier { StockMarket = g.Key.StockMarket, Code = g.Key.Code })
                .ToList();

            foreach (var request in distinctRequests)
            {
                var infoResult = await stockApiClients.GetStockInfoAsync(request.StockMarket, request.Code);

                if (!infoResult.IsSuccess || infoResult.Value == null)
                {
                    response.Failed.Add(new BatchStockInfoFailure
                    {
                        StockMarket = request.StockMarket,
                        Code = request.Code,
                        Message = "不支援此檔股票"
                    });
                    continue;
                }

                var info = infoResult.Value.Adapt<StockInfoResponse>();
                info.StockMarket = request.StockMarket;
                response.Succeeded.Add(info);
            }

            return Result<BatchStockInfoResponse>.Success(response);
        }

        /// <param name="requests">股票清單（市場 + 代碼）</param>
        /// <param name="asOf">截至日期；未指定時為 UTC 的今天</param>
        public async Task<Result<BatchStockPriceResponse>> GetLatestStockPricesAsync(List<StockIdentifier> requests, DateOnly? asOf)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var targetDate = asOf ?? today;

            if (targetDate > today)
            {
                return Result<BatchStockPriceResponse>.Failure(ResultCode.BusinessRuleViolation, "查詢日期不可大於今天");
            }

            var batchResult = new BatchStockPriceResponse();

            var distinctRequests = requests
                .GroupBy(r => (r.StockMarket, Code: r.Code.Trim().ToUpperInvariant()))
                .Select(g => new StockIdentifier { StockMarket = g.Key.StockMarket, Code = g.Key.Code })
                .ToList();

            // 先查 DB 快取，命中直接用，沒命中的進後續流程
            var (cachedPrices, notCached) = await TryGetFromCacheAsync(distinctRequests, targetDate);
            batchResult.Succeeded.AddRange(cachedPrices);

            // 驗證股票代碼是否支援
            var (validRequests, invalidFailures) = await ValidateStockInfosAsync(notCached);
            batchResult.Failed.AddRange(invalidFailures);

            var (allPrices, fetchFailures) = await FetchFromApiAsync(validRequests, targetDate);
            batchResult.Failed.AddRange(fetchFailures);

            // 只寫入 DB 還沒有的資料，避免約束衝突
            var newPrices = await FilterNewPricesAsync(allPrices);
            if (newPrices.Count > 0)
            {
                try
                {
                    dbContext.StockPriceHistories.AddRange(newPrices);
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, "寫入股價快取失敗。筆數: {Count}", newPrices.Count);
                }
            }

            // 截至日期是非交易日時，取最新一筆當作當天收盤價
            var latestPrices = allPrices
                .GroupBy(p => (p.StockMarket, p.Code))
                .Select(g => g.MaxBy(p => p.Date)!)
                .Select(p => p.Adapt<StockPriceResponse>())
                .ToList();

            batchResult.Succeeded.AddRange(latestPrices);

            return Result<BatchStockPriceResponse>.Success(batchResult);
        }

        private async Task<(List<StockPriceResponse> Cached, List<StockIdentifier> NotCached)> TryGetFromCacheAsync(
            List<StockIdentifier> distinctRequests, DateOnly targetDate)
        {
            var markets = distinctRequests.Select(r => r.StockMarket).Distinct().ToList();
            var codes = distinctRequests.Select(r => r.Code).Distinct().ToList();
            var requestKeys = distinctRequests.Select(r => (r.StockMarket, r.Code)).ToHashSet();

            // EF Core 不支援多欄位複合比對，改用兩個單欄位 IN 先撈回來，
            // 再用 HashSet 在記憶體精準過濾掉笛卡兒積的多餘組合
            var hits = (await dbContext.StockPriceHistories
                .Where(s => markets.Contains(s.StockMarket)
                         && codes.Contains(s.Code)
                         && s.Date == targetDate)
                .ToListAsync())
                .Where(s => requestKeys.Contains((s.StockMarket, s.Code)))
                .ToList();

            var hitKeys = hits.Select(s => (s.StockMarket, s.Code)).ToHashSet();
            var cached = hits.Select(s => s.Adapt<StockPriceResponse>()).ToList();
            var notCached = distinctRequests
                .Where(r => !hitKeys.Contains((r.StockMarket, r.Code)))
                .ToList();

            return (cached, notCached);
        }

        private async Task<(List<StockIdentifier> Valid, List<BatchStockPriceFailure> Failures)> ValidateStockInfosAsync(
            List<StockIdentifier> requests)
        {
            var valid = new List<StockIdentifier>();
            var failures = new List<BatchStockPriceFailure>();

            foreach (var request in requests)
            {
                var stockInfo = await stockApiClients.GetStockInfoAsync(request.StockMarket, request.Code);
                if (!stockInfo.IsSuccess || stockInfo.Value == null)
                {
                    failures.Add(new BatchStockPriceFailure
                    {
                        StockMarket = request.StockMarket,
                        Code = request.Code,
                        Message = "不支援此檔股票"
                    });
                    continue;
                }

                valid.Add(request);
            }

            return (valid, failures);
        }

        private async Task<(List<StockPriceHistory> AllPrices, List<BatchStockPriceFailure> Failures)> FetchFromApiAsync(
            List<StockIdentifier> requests, DateOnly targetDate)
        {
            var allPrices = new List<StockPriceHistory>();
            var failures = new List<BatchStockPriceFailure>();

            foreach (var request in requests)
            {
                var startDate = targetDate.AddDays(-7);
                var apiResult = await stockApiClients.GetStockPriceAsync(request.StockMarket, request.Code, startDate, targetDate);

                if (!apiResult.IsSuccess || apiResult.Value == null)
                {
                    failures.Add(new BatchStockPriceFailure
                    {
                        StockMarket = request.StockMarket,
                        Code = request.Code,
                        Message = apiResult.Message
                    });
                    continue;
                }

                if (apiResult.Value.Count < 1)
                {
                    failures.Add(new BatchStockPriceFailure
                    {
                        StockMarket = request.StockMarket,
                        Code = request.Code,
                        Message = "查無該期間的股價資料"
                    });
                    continue;
                }

                allPrices.AddRange(apiResult.Value);
            }

            return (allPrices, failures);
        }

        private async Task<List<StockPriceHistory>> FilterNewPricesAsync(List<StockPriceHistory> stockPrices)
        {
            if (stockPrices.Count == 0) return [];

            var targetDates = stockPrices.Select(price => price.Date).ToHashSet();
            var markets = stockPrices.Select(price => price.StockMarket).ToHashSet();
            var codes = stockPrices.Select(price => price.Code).ToHashSet();
            var requestKeys = stockPrices.Select(price => (price.StockMarket, price.Code, price.Date)).ToHashSet();

            var hits = (await dbContext.StockPriceHistories
                .Where(cachedPrice => markets.Contains(cachedPrice.StockMarket)
                                  && codes.Contains(cachedPrice.Code)
                                  && targetDates.Contains(cachedPrice.Date))
                .ToListAsync())
                .Where(cachedPrice => requestKeys.Contains((cachedPrice.StockMarket, cachedPrice.Code, cachedPrice.Date)))
                .ToList();

            var hitKeys = hits.Select(cachedPrice => (cachedPrice.StockMarket, cachedPrice.Code, cachedPrice.Date)).ToHashSet();
            var notCached = stockPrices
                .Where(price => !hitKeys.Contains((price.StockMarket, price.Code, price.Date)))
                .ToList();

            return notCached;
        }
    }
}
