using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

public class SettlementRequest
{
    [Description("還款人的成員ID")]
    public int FromMemberId { get; set; }

    [Description("收款人的成員ID")]
    public int ToMemberId { get; set; }

    [Description("還款金額，以基準幣計價")]
    public decimal Amount { get; set; }

    [Description("還款日期")]
    [Required(ErrorMessage = "請傳入還款日期")]
    public DateOnly? Date { get; set; }
}
