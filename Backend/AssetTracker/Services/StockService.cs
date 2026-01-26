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
        public async Task<Result<StockPriceHistory>> GetStockPrice(StockMarketType market, string code, DateTime date)
        {
            var normalizedCode = code.Trim().ToUpperInvariant();

            var stockInfo = await stockApiClients.GetStockInfo(market, normalizedCode);
            if (!stockInfo.IsSuccess || stockInfo.Value == null)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.BusinessRuleViolation, "不支援此檔股票");
            }

            var dbResult = await GetStockPriceFromDB(stockInfo.Value.Exchange, normalizedCode, date);

            if (dbResult.IsSuccess)
            {
                return dbResult;
            }

            var apiResult = await stockApiClients.GetStockPrice(market, normalizedCode, date, date);

            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                return Result<StockPriceHistory>.Failure(apiResult.Code, apiResult.Message);
            }

            if (apiResult.Value.Count < 1)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.NotFound, "查無此日的股價資料");
            }

            StockPriceHistory stockPriceHistory = apiResult.Value.First();

            dbContext.Add(stockPriceHistory);
            await dbContext.SaveChangesAsync();
            return Result<StockPriceHistory>.Success(stockPriceHistory);
        }

        private async Task<Result<StockPriceHistory>> GetStockPriceFromDB(string exchange, string code, DateTime date)
        {
            var stockPriceHistory = await dbContext.StockPriceHistories.FirstOrDefaultAsync(t => t.Exchange == exchange && t.Code == code && t.Date.Date == date.Date);

            if (stockPriceHistory == null)
            {
                return Result<StockPriceHistory>.Failure(ResultCode.NotFound, "資料庫查無此日的股票價格");
            }

            return Result<StockPriceHistory>.Success(stockPriceHistory);
        }
    }
}
