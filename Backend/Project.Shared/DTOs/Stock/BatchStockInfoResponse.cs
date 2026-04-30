using System.ComponentModel;

namespace Project.Shared.DTOs.Stock
{
    public class BatchStockInfoResponse
    {
        [Description("成功查詢的股票資訊")]
        public List<StockInfoResponse> Succeeded { get; set; } = [];

        [Description("失敗的股票清單")]
        public List<BatchStockInfoFailure> Failed { get; set; } = [];
    }
}
