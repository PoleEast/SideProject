using System.ComponentModel.DataAnnotations;

namespace Project.Api.Options
{
    /// <summary>
    /// 允許跨來源存取本 API 的前端來源。
    /// </summary>
    public class FrontendCorsOptions
    {
        public const string SectionName = "Cors";

        [Required(ErrorMessage = "缺少設定 'Cors:AllowedOrigins'。")]
        [MinLength(1, ErrorMessage = "設定 'Cors:AllowedOrigins' 至少需要一個允許的前端來源。")]
        public string[] AllowedOrigins { get; set; } = [];
    }
}
