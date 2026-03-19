using Project.Shared.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Project.Shared.DTOs.Transaction
{
    public class TransactionResponse
    {
        [Description("資源ID")]
        public int Id { get; set; }

        [Description("股票代碼")]
        public string StockCode { get; set; } = string.Empty;

        [Description("市場")]
        public StockMarketType Market { get; set; }

        [Description("交易日期")]
        public DateTime Date { get; set; }

        [Description("交易類型")]
        public TransactionType Type { get; set; }

        [Description("每單位金額")]
        public decimal Price { get; set; }

        [Description("數量")]
        public int Quantity { get; set; }

        [Description("備註")]
        public string Remark { get; set; } = string.Empty;

        [Description("資源創建日期")]
        public DateTime CreatedAt { get; set; }
    }
}
