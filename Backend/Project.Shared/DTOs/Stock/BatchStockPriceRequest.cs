using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.Stock
{
    public class BatchStockPriceRequest
    {
        [Description("市場")]
        [Required(ErrorMessage = "請傳入所屬市場")]
        public StockMarketType StockMarket { get; set; }

        [Description("股票代碼")]
        [Required(ErrorMessage = "請傳入股票代碼")]
        [StringLength(10, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;
    }
}
