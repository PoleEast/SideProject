using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 更新群組請求物件
/// </summary>
/// <remarks>
/// 邀請碼由重置端點管理、已結束狀態由專屬端點設定、擁有者不可變更，因此都不在此。
/// </remarks>
public class UpdateGroupRequest
{
    [Description("群組名稱")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "群組名稱長度請為 1~32 個字元")]
    public string? Name { get; set; }

    [Description("群組描述")]
    [StringLength(200, ErrorMessage = "群組描述請勿超過 200 個字元")]
    public string? Description { get; set; }

    /// <summary>
    /// 基準幣，僅在群組尚無任何花費與還款時可變更
    /// </summary>
    [Description("基準幣")]
    public CurrencyType? BaseCurrency { get; set; }
}
