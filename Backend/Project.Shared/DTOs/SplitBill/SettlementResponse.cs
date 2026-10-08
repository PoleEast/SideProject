using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

public class SettlementResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("還款人的成員ID")]
    public int FromMemberId { get; set; }

    [Description("收款人的成員ID")]
    public int ToMemberId { get; set; }

    [Description("還款金額，以基準幣計價")]
    public decimal Amount { get; set; }

    [Description("還款日期")]
    public DateOnly Date { get; set; }

    [Description("資源創建日期")]
    public DateTimeOffset CreatedAt { get; set; }
}
