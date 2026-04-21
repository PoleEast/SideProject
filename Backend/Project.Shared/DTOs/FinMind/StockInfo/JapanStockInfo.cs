namespace Project.Shared.DTOs.FinMind.StockInfo
{
    /// <summary>
    /// 日股股票基本資訊(來自FinMind API的JapanStockInfo資料集)
    /// </summary>
    public class JapanStockInfo
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
        /// 交易所
        /// </summary>
        public string Exchange { get; set; } = string.Empty;

        /// <summary>
        /// 產業類別
        /// </summary>
        public string Sector { get; set; } = string.Empty;

        /// <summary>
        /// 股票名稱
        /// </summary>
        public string StockName { get; set; } = string.Empty;
    }
}
