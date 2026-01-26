using System;

namespace Project.Shared.DTOs.FinMind.StockPrice
{
    /// <summary>
    /// 台股股價資訊(來自FinMind API的TaiwanStockPrice資料集)
    /// </summary>
    public class TaiwanStockPrice
    {
        /// <summary>
        /// 交易日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 股票代碼
        /// </summary>
        public string StockId { get; set; } = string.Empty;

        /// <summary>
        /// 成交股數
        /// </summary>
        public long TradingVolume { get; set; }

        /// <summary>
        /// 成交金額
        /// </summary>
        public decimal TradingMoney { get; set; }

        /// <summary>
        /// 開盤價
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// 最高價
        /// </summary>
        public decimal Max { get; set; }

        /// <summary>
        /// 最低價
        /// </summary>
        public decimal Min { get; set; }

        /// <summary>
        /// 收盤價
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// 漲跌幅
        /// </summary>
        public float Spread { get; set; }

        /// <summary>
        /// 成交筆數
        /// </summary>
        public int TradingTurnover { get; set; }
    }
}
