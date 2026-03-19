using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.Transaction
{
    public class UpdateTransactionRequest
    {
        [Description("股票代碼")]
        public string? StockCode { get; set; }

        [Description("市場")]
        public StockMarketType? Market { get; set; }

        [Description("交易日期")]
        public DateTime? Date { get; set; }

        [Description("交易類型")]
        public TransactionType? Type { get; set; }

        [Description("每單位金額")]
        [Range(0.01, double.MaxValue, ErrorMessage = "價格必須為正")]
        public decimal? Price { get; set; }

        [Description("數量")]
        [Range(1, int.MaxValue, ErrorMessage = "數量必須為正")]
        public int? Quantity { get; set; }

        [Description("備註")]
        public string? Remark { get; set; }
    }
}
