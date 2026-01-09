using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.Auth
{
    /// <summary>
    /// 註冊帳號物件
    /// </summary>
    public class RegisterRequest
    {
        [Description("帳號")]
        [Required(ErrorMessage = "請傳入註冊的帳號")]
        [StringLength(maximumLength: 30, MinimumLength = 6, ErrorMessage = "帳號長度請為6~30個字元")]
        public string Account { get; set; } = string.Empty;

        [Description("密碼")]
        [Required(ErrorMessage = "請傳入註冊的密碼")]
        [StringLength(maximumLength: 30, MinimumLength = 6, ErrorMessage = "密碼長度請為6~30個字元")]
        public string Password { get; set; } = string.Empty;

        [Description("使用者名稱")]
        [Required(ErrorMessage = "請傳入使用者名稱")]
        [StringLength(maximumLength: 30, MinimumLength = 2, ErrorMessage = "名稱長度請為2~30個字元")]
        public string Name { get; set; } = string.Empty;
    }
}
