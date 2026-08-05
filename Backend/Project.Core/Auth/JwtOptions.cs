using System.ComponentModel.DataAnnotations;

namespace Project.Core.Auth
{
    /// <summary>
    /// JWT 簽發與驗證所需的設定。
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required(ErrorMessage = "缺少設定 'Jwt:Key'，請參考 secrets.sample.json。")]
        [MinLength(32, ErrorMessage = "設定 'Jwt:Key' 至少需要 32 個字元，HS256 要求金鑰長度不低於 256 bits。")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "缺少設定 'Jwt:Issuer'。")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "缺少設定 'Jwt:Audience'。")]
        public string Audience { get; set; } = string.Empty;
    }
}
