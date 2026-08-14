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
        public async Task<Result<StockPriceResponse>> GetLatestStockPriceAsync(StockMarketType market, string code, DateTime asOf)
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

        public async Task<Result<BatchStockPriceResponse>> GetLatestStockPricesAsync(List<StockIdentifier> requests, DateTime asOf)
        {
            if (asOf.Date > DateTime.Today)
            {
                return Result<BatchStockPriceResponse>.Failure(ResultCode.BusinessRuleViolation, "查詢日期不可大於今天");
            }

            var batchResult = new BatchStockPriceResponse();

            var distinctRequests = requests
                .GroupBy(r => (r.StockMarket, Code: r.Code.Trim().ToUpperInvariant()))
                .Select(g => new StockIdentifier { StockMarket = g.Key.StockMarket, Code = g.Key.Code })
                .ToList();

            // 先查 DB 快取，命中直接用，沒命中的進後續流程
            var (cachedPrices, notCached) = await TryGetFromCacheAsync(distinctRequests, asOf);
            batchResult.Succeeded.AddRange(cachedPrices);

            // 驗證股票代碼是否支援
            var (validRequests, invalidFailures) = await ValidateStockInfosAsync(notCached);
            batchResult.Failed.AddRange(invalidFailures);

            var (allPrices, fetchFailures) = await FetchFromApiAsync(validRequests, asOf);
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

            // asOf是非交易日時，取最新一筆當作當天收盤價
            var latestPrices = allPrices
                .GroupBy(p => (p.StockMarket, p.Code))
                .Select(g => g.MaxBy(p => p.Date)!)
                .Select(p => p.Adapt<StockPriceResponse>())
                .ToList();

            batchResult.Succeeded.AddRange(latestPrices);

            return Result<BatchStockPriceResponse>.Success(batchResult);
        }

        private async Task<(List<StockPriceResponse> Cached, List<StockIdentifier> NotCached)> TryGetFromCacheAsync(
            List<StockIdentifier> distinctRequests, DateTime asOf)
        {
            var targetDate = asOf.Date;
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
            List<StockIdentifier> requests, DateTime asOf)
        {
            var allPrices = new List<StockPriceHistory>();
            var failures = new List<BatchStockPriceFailure>();

            foreach (var request in requests)
            {
                var startDate = asOf.AddDays(-7);
                var apiResult = await stockApiClients.GetStockPriceAsync(request.StockMarket, request.Code, startDate, asOf);

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

            var targetDates = stockPrices.Select(s => s.Date.Date).ToHashSet();
            var markets = stockPrices.Select(s => s.StockMarket).ToHashSet();
            var codes = stockPrices.Select(r => r.Code).ToHashSet();
            var requestKeys = stockPrices.Select(r => (r.StockMarket, r.Code, r.Date.Date)).ToHashSet();

            var hits = (await dbContext.StockPriceHistories
                .Where(s => markets.Contains(s.StockMarket)
                         && codes.Contains(s.Code)
                         && targetDates.Contains(s.Date))
                .ToListAsync())
                .Where(s => requestKeys.Contains((s.StockMarket, s.Code, s.Date.Date)))
                .ToList();

            var hitKeys = hits.Select(s => (s.StockMarket, s.Code, s.Date.Date)).ToHashSet();
            var notCached = stockPrices
                .Where(r => !hitKeys.Contains((r.StockMarket, r.Code, r.Date.Date)))
                .ToList();

            return notCached;
        }
    }
}
