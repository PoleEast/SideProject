using Mapster;
using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.DTOs.ExchangeRate.ExchangeRateAPI;
using Project.Shared.Types;
using System.Text.Json;

namespace Project.Api.ApiClients
{
    public class ExchangeRateApiClient(HttpClient httpClient, [FromKeyedServices("ApiResponse")] JsonSerializerOptions options, ILogger<ExchangeRateApiClient> logger) : IExchangeRateApiClient
    {
        public async Task<Result<ExchangeRateResponse>> FetchStandardRequestsAsync(CurrencyType baseCode)
        {
            var url = $"latest/{baseCode}";

            StandardResponse? response;
            try
            {
                response = await httpClient.GetFromJsonAsync<StandardResponse>(url, options);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "呼叫匯率 API 失敗。baseCode: {BaseCode}", baseCode);
                return Result<ExchangeRateResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供");
            }

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

