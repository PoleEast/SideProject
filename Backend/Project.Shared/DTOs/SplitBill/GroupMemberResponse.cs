using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 群組成員回應物件
/// </summary>
public class GroupMemberResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("顯示名稱")]
    public string DisplayName { get; set; } = string.Empty;

    [Description("是否已移除")]
    public bool IsRemoved { get; set; }

    [Description("是否為呼叫者本人")]
    public bool IsMe { get; set; }

    [Description("是否為擁有者的位置")]
    public bool IsOwner { get; set; }

    [Description("是否已綁定帳號")]
    public bool IsBound { get; set; }
}
