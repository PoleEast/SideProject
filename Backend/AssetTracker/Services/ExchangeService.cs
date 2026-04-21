using AssetTracker.ApiClients;
using Microsoft.Extensions.Caching.Memory;
using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.Types;

namespace AssetTracker.Services
{
    public class ExchangeRateService(IExchangeRateApiClient exchangeRateApiClient, IMemoryCache cache)
    {
        public async Task<Result<ExchangeRateResponse>> GetExchangeRateAsync(CurrencyType currencyType)
        {
            var cacheKey = $"{currencyType}_ExchangeRate";
            if (cache.TryGetValue(cacheKey, out ExchangeRateResponse? cached))
            {
                return Result<ExchangeRateResponse>.Success(cached);
            }

            var apiResult = await exchangeRateApiClient.FetchStandardRequestsAsync(currencyType);
            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                return apiResult;
            }

            cache.Set(cacheKey, apiResult.Value, TimeSpan.FromMinutes(5));

            return apiResult;
        }
    }
}
