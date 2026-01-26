using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.Types;

namespace AssetTracker.ApiClients
{
    public interface IExchangeRateApiClient
    {
        public Task<Result<ExchangeRateHistory>> GetExchangeRateToUSD(CurrencyType baseCode);
    }
}
