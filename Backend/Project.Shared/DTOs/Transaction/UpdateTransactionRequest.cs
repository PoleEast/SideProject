using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.Transaction
{
    public class UpdateTransactionRequest
    {
        [Description("股票代碼")]
        public string? StockCode { get; set; } = string.Empty;

        [Description("交易所")]
        public string? Exchange { get; set; } = string.Empty;

        [Description("交易日期")]
        public DateTime? Date { get; set; }

        [Description("交易類型")]
        public TransactionType? Type { get; set; }

        [Description("每單位金額")]
        [Range(0.0, double.MaxValue, ErrorMessage = "價格必須為正")]
        public decimal? Price { get; set; }

        [Description("數量")]
        [Range(0, int.MaxValue, ErrorMessage = "數量必須為正")]
        public int? Quantity { get; set; }

        [Description("貨幣代碼")]
        public string? CurrencyCode { get; set; } = string.Empty;

        [Description("備註")]
        public string? Remark { get; set; } = string.Empty;
    }
}
