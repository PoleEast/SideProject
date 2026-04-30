using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.Stock
{
    public class StockInfoResponse
    {
        [Description("市場")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        public string Code { get; set; } = string.Empty;

        [Description("股票名稱")]
        public string Name { get; set; } = string.Empty;

        [Description("產業類別")]
        public string IndustryCategory { get; set; } = string.Empty;

        [Description("交易所")]
        public string Exchange { get; set; } = string.Empty;
    }
}
