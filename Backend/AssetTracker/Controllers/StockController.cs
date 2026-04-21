using AssetTracker.Common;
using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Project.Shared.DTOs.Stock;
using Project.Shared.Types;
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController(StockService stockService) : ControllerBase
    {
        /// <summary>
        /// 查詢截至指定日期為止最新的股價（非交易日會回傳前一交易日收盤價）
        /// </summary>
        /// <param name="stockMarketType">市場類型：TW=台股, US=美股, JP=日股</param>
        /// <param name="code">股票代碼（如：2330、AAPL、7203）</param>
        /// <param name="asOf">截至日期（不可大於今天）</param>
        [HttpGet("latest-price")]
        public async Task<ActionResult<StockPriceResponse>> GetLatestStockPrice([BindRequired] StockMarketType stockMarketType, [BindRequired, StringLength(10, MinimumLength = 1)] string code, [BindRequired, NotFutureDate] DateTime asOf)
        {
            var result = await stockService.GetLatestStockPriceAsync(stockMarketType, code, asOf);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                ResultCode.NotFound => NotFound(result.Message),
                _ => StatusCode((int)result.Code)
            };
        }

        /// <summary>
        /// 批次查詢多檔股票截至指定日期為止最新的股價，回傳成功與失敗清單
        /// </summary>
        /// <param name="requests">股票清單（市場 + 代碼）</param>
        /// <param name="asOf">截至日期（不可大於今天）</param>
        [HttpPost("latest-prices")]
        public async Task<ActionResult<BatchStockPriceResponse>> GetLatestStockPrices([FromBody, Length(1, 50)] List<BatchStockPriceRequest> requests, [FromQuery][BindRequired, NotFutureDate] DateTime asOf)
        {
            var result = await stockService.GetLatestStockPricesAsync(requests, asOf);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                // 批次查詢單檔失敗會進 Failed 清單，不會整批回 NotFound
                _ => StatusCode((int)result.Code)
            };
        }
    }
}
