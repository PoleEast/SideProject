using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.Stock
{
    public class BatchStockInfoFailure
    {
        [Description("市場")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        public string Code { get; set; } = string.Empty;

        [Description("失敗原因")]
        public string Message { get; set; } = string.Empty;
    }
}
