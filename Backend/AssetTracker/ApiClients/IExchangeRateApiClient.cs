using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.Types;

namespace AssetTracker.ApiClients
{
    public interface IExchangeRateApiClient
    {
        public Task<Result<ExchangeRateResponse>> FetchStandardRequestsAsync(CurrencyType baseCode);
    }
}
