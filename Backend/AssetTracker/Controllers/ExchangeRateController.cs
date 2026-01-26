using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Shared.Types;

namespace AssetTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExchangeRateController(ExchangeRateService exchangeRateService) : ControllerBase
    {
        [HttpGet("{currencyType}")]
        public async Task<ActionResult> GetExchangeRate(CurrencyType currencyType)
        {
            var result = await exchangeRateService.GetExchangeRateAsync(currencyType);

            return result.Code switch
            {
                ResultCode.Success => Ok(result),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                ResultCode.ExternalApiError => StatusCode(502, result.Message),
                _ => StatusCode((int)result.Code)
            };
        }
    }
}