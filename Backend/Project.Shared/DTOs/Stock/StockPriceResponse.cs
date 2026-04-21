using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.Stock
{
    public class StockPriceResponse
    {
        [Description("市場")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        public string Code { get; set; } = string.Empty;

        [Description("股票名稱")]
        public string Name { get; set; } = string.Empty;

        [Description("收盤價")]
        public decimal ClosingPrice { get; set; }

        [Description("幣別")]
        public CurrencyType Currency { get; set; }

        [Description("日期")]
        public DateTime Date { get; set; }
    }
}
