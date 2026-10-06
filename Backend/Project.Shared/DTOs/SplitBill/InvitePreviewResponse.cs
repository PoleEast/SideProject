using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

public class InvitePreviewResponse
{
    [Description("群組ID")]
    public int GroupId { get; set; }

    [Description("群組名稱")]
    public string GroupName { get; set; } = string.Empty;

    [Description("呼叫者是否已是此群組的成員")]
    public bool IsMember { get; set; }

    [Description("以新成員加入時預填的顯示名稱")]
    public string SuggestedDisplayName { get; set; } = string.Empty;

    [Description("未移除的成員，依ID排序")]
    public List<InvitePreviewMemberResponse> Members { get; set; } = [];
}

public class InvitePreviewMemberResponse
{
    [Description("成員ID")]
    public int Id { get; set; }

    [Description("顯示名稱")]
    public string DisplayName { get; set; } = string.Empty;

    [Description("是否已綁定帳號")]
    public bool IsBound { get; set; }
}
