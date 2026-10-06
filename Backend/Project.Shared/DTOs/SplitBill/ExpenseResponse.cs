using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

public class ExpenseResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("花費名稱")]
    public string Name { get; set; } = string.Empty;

    [Description("花費描述")]
    public string Description { get; set; } = string.Empty;

    [Description("分類")]
    public ExpenseCategoryType Category { get; set; }

    [Description("原幣")]
    public CurrencyType Currency { get; set; }

    [Description("原幣金額")]
    public decimal Amount { get; set; }

    [Description("原幣對基準幣的匯率")]
    public decimal Rate { get; set; }

    [Description("消費日期")]
    public DateOnly Date { get; set; }

    [Description("付款人的成員ID")]
    public int PayerId { get; set; }

    [Description("分攤，依成員ID排序")]
    public List<ExpenseShareResponse> Shares { get; set; } = [];

    [Description("資源創建日期")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class ExpenseShareResponse
{
    [Description("參與者的成員ID")]
    public int MemberId { get; set; }

    [Description("分攤額，以原幣計價")]
    public decimal Amount { get; set; }
}
