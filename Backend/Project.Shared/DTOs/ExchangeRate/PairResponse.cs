using System;

namespace Project.Shared.DTOs.ExchangeRate
{
    /// <summary>
    /// ExchangeRate-API Pair Conversion 回應格式
    /// </summary>
    public class PairResponse
    {
        /// <summary>
        /// API 請求結果 (success/error)
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// 錯誤類型 (當 Result 為 error 時會有值)
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// API 文件網址
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// 使用條款網址
        /// </summary>
        public string TermsOfUse { get; set; } = string.Empty;

        /// <summary>
        /// 上次更新時間 (Unix 時間戳)
        /// </summary>
        public long TimeLastUpdateUnix { get; set; }

        /// <summary>
        /// 上次更新時間 (UTC 格式)
        /// </summary>
        public string TimeLastUpdateUtc { get; set; } = string.Empty;

        /// <summary>
        /// 下次更新時間 (Unix 時間戳)
        /// </summary>
        public long TimeNextUpdateUnix { get; set; }

        /// <summary>
        /// 下次更新時間 (UTC 格式)
        /// </summary>
        public string TimeNextUpdateUtc { get; set; } = string.Empty;

        /// <summary>
        /// 基準貨幣代碼 (如 TWD, USD)
        /// </summary>
        public string BaseCode { get; set; } = string.Empty;

        /// <summary>
        /// 目標貨幣代碼 (如 USD, EUR)
        /// </summary>
        public string TargetCode { get; set; } = string.Empty;

        /// <summary>
        /// 匯率 (1 基準貨幣 = ? 目標貨幣)
        /// </summary>
        public decimal ConversionRate { get; set; }
    }
}
