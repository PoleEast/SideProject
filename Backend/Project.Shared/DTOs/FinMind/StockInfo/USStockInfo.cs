using System;

namespace Project.Shared.DTOs.FinMind.StockInfo
{
    /// <summary>
    /// 美股股票基本資訊(來自FinMind API的USStockInfo資料集)
    /// </summary>
    public class USStockInfo
    {
        /// <summary>
        /// 資料日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 股票代碼
        /// </summary>
        public string StockId { get; set; } = string.Empty;

        /// <summary>
        /// 國家
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// 首次公開發行年份
        /// </summary>
        public string IPOYear { get; set; } = string.Empty;

        /// <summary>
        /// 市值
        /// </summary>
        public string MarketCap { get; set; } = string.Empty;

        /// <summary>
        /// 產業子類別
        /// </summary>
        public string Subsector { get; set; } = string.Empty;

        /// <summary>
        /// 股票名稱
        /// </summary>
        public string StockName { get; set; } = string.Empty;
    }
}
