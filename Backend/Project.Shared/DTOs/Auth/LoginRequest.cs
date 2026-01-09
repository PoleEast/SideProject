using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Project.Shared.DTOs.Auth
{
    /// <summary>
    /// 登入請求物件
    /// </summary>
    public class LoginRequest
    {
        [Description("帳號")]
        [Required(ErrorMessage = "請傳入登入帳號")]
        public string Account { get; set; } = string.Empty;

        [Description("密碼")]
        [Required(ErrorMessage = "請傳入登入密碼")]
        public string Password { get; set; } = string.Empty;
    }
}
