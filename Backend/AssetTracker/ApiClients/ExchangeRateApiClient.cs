using Mapster;
using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.DTOs.ExchangeRate.ExchangeRateAPI;
using Project.Shared.Types;
using System.Text.Json;

namespace AssetTracker.ApiClients
{
    public class ExchangeRateApiClient(HttpClient httpClient, [FromKeyedServices("ApiResponse")] JsonSerializerOptions options) : IExchangeRateApiClient
    {
        public async Task<Result<ExchangeRateResponse>> FetchStandardRequestsAsync(CurrencyType baseCode)
        {
            var url = $"latest/{baseCode}";

            var response = await httpClient.GetFromJsonAsync<StandardResponse>(url, options);

            if (response == null) return Result<ExchangeRateResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供");

            if (!string.IsNullOrEmpty(response.ErrorType))
            {
                return response.ErrorType switch
                {
                    "unsupported-code" => Result<ExchangeRateResponse>.Failure(ResultCode.BusinessRuleViolation, "不支援提供的貨幣代碼"),
                    _ => Result<ExchangeRateResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供")
                };
            }

            var result = response.Adapt<ExchangeRateResponse>();

            return Result<ExchangeRateResponse>.Success(result);
        }
    }
}

