using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Stock;
using Project.Shared.Types;
using static AssetTracker.Common.StockConfig;

namespace AssetTracker.ApiClients
{
    public interface IStockApiClients
    {
        public Task<Result<List<StockPriceHistory>>> GetStockPrice(StockMarketType market, string code, DateTime startDate, DateTime endDate);

        public Task<Result<StockInfo>> GetStockInfo(StockMarketType market, string code);
    }
}
