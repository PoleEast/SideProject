using System;

namespace Project.Shared.DTOs.FinMind.StockInfo
{
    /// <summary>
    /// 台股股票基本資訊(來自FinMind API的TaiwanStockInfo資料集)
    /// </summary>
    public class TaiwanStockInfo
    {
        /// <summary>
        /// 產業類別
        /// </summary>
        public string IndustryCategory { get; set; } = string.Empty;

        /// <summary>
        /// 股票代碼
        /// </summary>
        public string StockId { get; set; } = string.Empty;

        /// <summary>
        /// 股票名稱
        /// </summary>
        public string StockName { get; set; } = string.Empty;

        /// <summary>
        /// 股票類型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 資料日期
        /// </summary>
        public DateTime Date { get; set; }
    }
}
