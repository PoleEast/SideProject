using Mapster;
using Microsoft.AspNetCore.WebUtilities;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.Types;
using System;
using System.Text.Json;

namespace AssetTracker.ApiClients
{
    public class ExchangeRateApiClient(HttpClient httpClient, [FromKeyedServices("ApiResponse")] JsonSerializerOptions options) : IExchangeRateApiClient
    {
        public async Task<Result<ExchangeRateHistory>> GetExchangeRateToUSDAsync(CurrencyType baseCode)
        {
            var pairResponse = await FetchPairConversionAsync(baseCode, CurrencyType.USD);

            if (!pairResponse.IsSuccess || pairResponse.Value == null) return Result<ExchangeRateHistory>.Failure(pairResponse.Code, pairResponse.Message);

            var exchangeRateHistory = pairResponse.Value.Adapt<ExchangeRateHistory>();

            return Result<ExchangeRateHistory>.Success(exchangeRateHistory);
        }

        private async Task<Result<PairResponse>> FetchPairConversionAsync(CurrencyType baseCode, CurrencyType targetCode)
        {
            var url = $"pair/{baseCode}/{targetCode}";

            var response = await httpClient.GetFromJsonAsync<PairResponse>(url, options);

            if (response == null) return Result<PairResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供");

            if (!string.IsNullOrEmpty(response.ErrorType))
            {
                return response.ErrorType switch
                {
                    "unsupported-code" => Result<PairResponse>.Failure(ResultCode.BusinessRuleViolation, "不支援提供的貨幣代碼"),
                    _ => Result<PairResponse>.Failure(ResultCode.ExternalApiError, "服務暫時無法提供")
                };
            }

            return Result<PairResponse>.Success(response);
        }
    }
}
