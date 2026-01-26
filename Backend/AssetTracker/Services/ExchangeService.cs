using AssetTracker.ApiClients;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.Types;

namespace AssetTracker.Services
{
    public class ExchangeRateService(IExchangeRateApiClient exchangeRateApiClient, ApplicationDbContext dbContext)
    {
        public async Task<Result<ExchangeRateHistory>> GetExchangeRateAsync(CurrencyType currencyType)
        {
            var dbResult = await dbContext.ExchangeRateHistories.FirstOrDefaultAsync(e => e.CurrencyCode == currencyType.ToString() && e.Date.Date == DateTime.UtcNow.Date);
            if (dbResult != null) return Result<ExchangeRateHistory>.Success(dbResult);

            var apiResult = await exchangeRateApiClient.GetExchangeRateToUSDAsync(currencyType);
            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                return apiResult;
            }

            dbContext.ExchangeRateHistories.Add(apiResult.Value);
            await dbContext.SaveChangesAsync();

            return apiResult;
        }
    }
}
