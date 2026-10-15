using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

public class GroupListItemResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("群組名稱")]
    public string Name { get; set; } = string.Empty;

    [Description("群組圖示")]
    public GroupIconType Icon { get; set; }

    [Description("基準幣")]
    public CurrencyType BaseCurrency { get; set; }

    [Description("已結束時間")]
    public DateTimeOffset? ClosedAt { get; set; }

    [Description("現役成員數")]
    public int ActiveMemberCount { get; set; }

    [Description("呼叫者的個人淨額，以基準幣表示；正數是會收回，負數是要付")]
    public decimal PersonalNet { get; set; }

    [Description("呼叫者是否已結清")]
    public bool IsSettled { get; set; }

    [Description("群組是否全員結清")]
    public bool IsEveryoneSettled { get; set; }

    [Description("最近一則群組動態的時間")]
    public DateTimeOffset LastActivityAt { get; set; }
}
