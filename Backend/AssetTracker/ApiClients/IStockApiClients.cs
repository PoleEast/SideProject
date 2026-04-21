using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Stock;
using Project.Shared.Types;

namespace AssetTracker.ApiClients
{
    public interface IStockApiClients
    {
        public Task<Result<List<StockPriceHistory>>> GetStockPriceAsync(StockMarketType market, string code, DateTime startDate, DateTime endDate);

        public Task<Result<StockInfo>> GetStockInfoAsync(StockMarketType market, string code);
    }
}
