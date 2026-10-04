using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 以新成員加入請求物件
/// </summary>
public class JoinAsNewMemberRequest
{
    [Description("顯示名稱")]
    [Required(ErrorMessage = "請傳入顯示名稱")]
    public string DisplayName { get; set; } = string.Empty;
}
