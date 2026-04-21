namespace Project.Shared.DTOs.Stock
{
    /// <summary>
    /// 統一的股票價格資訊DTO(從各市場API回傳轉換而來)
    /// </summary>
    public class StockPrice
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
    }
}
