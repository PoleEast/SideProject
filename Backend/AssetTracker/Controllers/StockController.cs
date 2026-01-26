using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        //TODO:參數驗證

        /// <summary>
        /// 查詢指定日期的股價
        /// </summary>
        /// <param name="stockMarketType">市場類型：TW=台股, US=美股, JP=日股</param>
        /// <param name="code">股票代碼（如：2330、AAPL、7203）</param>
        /// <param name="date">查詢日期</param>
        [HttpGet]
        public async Task<ActionResult<StockPriceHistory>> GetStockPrice(StockMarketType stockMarketType, string code, DateTime date)
        {
            var result = await stockService.GetStockPrice(stockMarketType, code, date);

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
