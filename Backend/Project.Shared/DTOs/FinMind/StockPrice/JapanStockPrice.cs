namespace Project.Shared.DTOs.FinMind.StockPrice
{
    /// <summary>
    /// 日股股價資訊(來自FinMind API的JapanStockPrice資料集)
    /// </summary>
    public class JapanStockPrice
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
        /// 調整後收盤價（已考慮股息、股票分割等因素）
        /// </summary>
        public decimal AdjClose { get; set; }

        /// <summary>
        /// 收盤價
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// 最高價
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// 最低價
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// 開盤價
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// 成交量
        /// </summary>
        public long Volume { get; set; }
    }
}
