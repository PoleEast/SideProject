using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 群組回應物件
/// </summary>
public class GroupResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("群組名稱")]
    public string Name { get; set; } = string.Empty;

    [Description("群組描述")]
    public string Description { get; set; } = string.Empty;

    [Description("基準幣")]
    public CurrencyType BaseCurrency { get; set; }

    [Description("邀請碼")]
    public string InviteCode { get; set; } = string.Empty;

    [Description("擁有者的使用者ID")]
    public int OwnerUserId { get; set; }

    /// <summary>
    /// 已結束時間。有值代表已結束，僅影響前端分區，不限制任何操作
    /// </summary>
    [Description("已結束時間")]
    public DateTimeOffset? ClosedAt { get; set; }

    [Description("資源創建日期")]
    public DateTimeOffset CreatedAt { get; set; }
}
