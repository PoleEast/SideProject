using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.Position
{
    /// <summary>
    /// 持倉資訊
    /// </summary>
    public class PositionResponse
    {
        [Description("股票市場類型")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        public string StockCode { get; set; } = string.Empty;

        [Description("持有數量")]
        public int Quantity { get; set; }

        [Description("平均成本")]
        public decimal AveragePrice { get; set; }
    }
}
