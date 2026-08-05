using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Options
{
    /// <summary>
    /// FinMind 股價 API 的連線設定。
    /// </summary>
    public class FinMindOptions
    {
        public const string SectionName = "FinMindApi";

        [Required(ErrorMessage = "缺少設定 'FinMindApi:BaseApi'。")]
        [Url(ErrorMessage = "設定 'FinMindApi:BaseApi' 必須是符合格式的 URL。")]
        public string BaseApi { get; set; } = string.Empty;

        [Required(ErrorMessage = "缺少設定 'FinMindApi:Key'，請參考 secrets.sample.json 設定。")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// HttpClient.BaseAddress 若不以斜線結尾，相對路徑會取代掉最後一段路徑
        /// （例如 .../api/v4 + "data" 會變成 .../api/data），因此統一補上。
        /// </summary>
        public string NormalizedBaseApi => BaseApi.EndsWith('/') ? BaseApi : BaseApi + "/";
    }
}
