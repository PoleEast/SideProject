using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Options
{
    /// <summary>
    /// ExchangeRate-API 的連線設定。此 API 將金鑰放在 URL 路徑而非 header，
    /// 因此 Key 會與 BaseApi 組成 HttpClient 的 BaseAddress。
    /// </summary>
    public class ExchangeRateOptions
    {
        public const string SectionName = "ExchangeRateApi";

        [Required(ErrorMessage = "缺少設定 'ExchangeRateApi:BaseApi'")]
        [Url(ErrorMessage = "設定 'ExchangeRateApi:BaseApi' 必須是符合格式的 URL。")]
        public string BaseApi { get; set; } = string.Empty;

        [Required(ErrorMessage = "缺少設定 'ExchangeRateApi:Key'，請參考 secrets.sample.json 設定。")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 組出 https://{host}/v6/{key}/ 形式的 BaseAddress。
        /// </summary>
        public string BaseAddressWithKey =>
            (BaseApi.EndsWith('/') ? BaseApi : BaseApi + "/") + Key + "/";
    }
}
