using Project.Api.ApiClients;
using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.Types;

namespace Project.Tests.Helpers;

/// <summary>
/// 匯率 API 用戶端的測試替身
/// </summary>
/// <remarks>
/// 只取代對外的 HTTP 呼叫；快取與取值仍由真正的匯率服務執行。
/// </remarks>
public class FakeExchangeRateApiClient : IExchangeRateApiClient
{
    private readonly Dictionary<CurrencyType, Dictionary<CurrencyType, decimal>> ratesBySource = [];

    public int CallCount { get; private set; }

    /// <summary>
    /// 設為 true 時每次呼叫都回傳失敗
    /// </summary>
    public bool IsFailing { get; set; }

    public void SetRate(CurrencyType source, CurrencyType target, decimal rate)
    {
        if (!ratesBySource.TryGetValue(source, out var conversionRates))
        {
            conversionRates = [];
            ratesBySource[source] = conversionRates;
        }

        conversionRates[target] = rate;
    }

    public Task<Result<ExchangeRateResponse>> FetchStandardRequestsAsync(CurrencyType baseCode)
    {
        CallCount++;

        if (IsFailing)
        {
            return Task.FromResult(Result<ExchangeRateResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供"));
        }

        var response = new ExchangeRateResponse
        {
            Currency = baseCode,
            ConversionRates = ratesBySource.GetValueOrDefault(baseCode) ?? []
        };

        return Task.FromResult(Result<ExchangeRateResponse>.Success(response));
    }
}
