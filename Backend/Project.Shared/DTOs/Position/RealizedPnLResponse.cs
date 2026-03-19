using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.Position
{
    /// <summary>
    /// 已實現損益資訊
    /// </summary>
    public class RealizedPnLResponse
    {
        [Description("資源ID")]
        public int Id { get; set; }

        [Description("股票市場類型")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        public string StockCode { get; set; } = string.Empty;

        [Description("交易日期")]
        public DateTime Date { get; set; }

        [Description("賣出數量")]
        public int SellQuantity { get; set; }

        [Description("賣出均價")]
        public decimal SellPrice { get; set; }

        [Description("買入均價")]
        public decimal BuyPrice { get; set; }
    }
}
