using AssetTracker.Common;
using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Project.Data.Model;
using Project.Shared.Types;
using System.Net;
using System.Threading.Tasks;
using static AssetTracker.Common.StockConfig;

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
        public async Task<ActionResult<StockPriceHistory>> GetLatestStockPrice([BindRequired] StockMarketType stockMarketType, [BindRequired, StringLength(10, MinimumLength = 1)] string code, [BindRequired, NotFutureDate] DateTime asOf)
        {
            var result = await stockService.GetLatestStockPriceAsync(stockMarketType, code, asOf);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                ResultCode.NotFound => BadRequest(result.Message),
                _ => StatusCode((int)result.Code)
            };
        }
    }
}
