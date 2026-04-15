using AssetTracker.ApiClients;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.Types;
using static AssetTracker.Common.StockConfig;

namespace AssetTracker.Services
{
    public class StockService(IStockApiClients stockApiClients, ApplicationDbContext dbContext)
    {
        public async Task<Result<StockPriceHistory>> GetLatestStockPriceAsync(StockMarketType market, string code, DateTime asOf)
        {
            if (asOf.Date > DateTime.Today)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.BusinessRuleViolation, "查詢日期不可大於今天");
            }

            var normalizedCode = code.Trim().ToUpperInvariant();

            var stockInfo = await stockApiClients.GetStockInfoAsync(market, normalizedCode);
            if (!stockInfo.IsSuccess || stockInfo.Value == null)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.BusinessRuleViolation, "不支援此檔股票");
            }

            var existing = await GetStockPriceFromDBAsync(stockInfo.Value.Exchange, normalizedCode, asOf);

            if (existing != null)
            {
                return Result<StockPriceHistory>.Success(existing);
            }

            var startDate = asOf.AddDays(-7);
            var apiResult = await stockApiClients.GetStockPriceAsync(market, normalizedCode, startDate, asOf);

            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                return Result<StockPriceHistory>.Failure(apiResult.Code, apiResult.Message);
            }

            if (apiResult.Value.Count < 1)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.NotFound, "查無該期間的股價資料");
            }

            StockPriceHistory stockPriceHistory = apiResult.Value.OrderBy(v => v.Date).Last();

            var duplicate = await GetStockPriceFromDBAsync(stockInfo.Value.Exchange, normalizedCode, stockPriceHistory.Date);

            if (duplicate == null)
            {
                dbContext.Add(stockPriceHistory);
                await dbContext.SaveChangesAsync();
            }

            return Result<StockPriceHistory>.Success(stockPriceHistory);
        }

        private async Task<StockPriceHistory?> GetStockPriceFromDBAsync(string exchange, string code, DateTime date)
        {
            return await dbContext.StockPriceHistories
                .FirstOrDefaultAsync(t => t.Exchange == exchange && t.Code == code && t.Date.Date == date.Date);
        }
    }
}
