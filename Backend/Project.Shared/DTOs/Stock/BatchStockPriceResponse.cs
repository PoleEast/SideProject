using System.ComponentModel;

namespace Project.Shared.DTOs.Stock
{
    public class BatchStockPriceResponse
    {
        [Description("成功查詢的股價")]
        public List<StockPriceResponse> Succeeded { get; set; } = [];

        [Description("失敗的股票清單")]
        public List<BatchStockPriceFailure> Failed { get; set; } = [];
    }
}
