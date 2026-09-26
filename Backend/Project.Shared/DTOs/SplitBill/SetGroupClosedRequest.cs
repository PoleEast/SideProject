using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 設定群組已結束狀態請求物件
/// </summary>
public class SetGroupClosedRequest
{
    [Description("是否已結束")]
    [Required(ErrorMessage = "請傳入是否已結束")]
    public bool? Closed { get; set; }
}
