using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.Types;

namespace Project.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExchangeRateController(ExchangeRateService exchangeRateService) : ControllerBase
    {
        [HttpGet("{currencyType}")]
        public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRate(CurrencyType currencyType)
        {
            var result = await exchangeRateService.GetExchangeRateAsync(currencyType);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }
    }
}