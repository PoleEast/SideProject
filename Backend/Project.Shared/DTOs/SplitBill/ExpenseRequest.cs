using Project.Shared.Constants;
using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

/// <summary>
/// 花費內容請求物件，建立與編輯共用
/// </summary>
public class ExpenseRequest
{
    [Description("花費名稱")]
    [Required(ErrorMessage = "請傳入花費名稱")]
    [StringLength(MaxLengths.ExpenseName, ErrorMessage = "花費名稱請勿超過 {1} 個字元")]
    public string Name { get; set; } = string.Empty;

    [Description("花費描述")]
    [StringLength(MaxLengths.ExpenseDescription, ErrorMessage = "花費描述請勿超過 {1} 個字元")]
    public string Description { get; set; } = string.Empty;

    [Description("分類")]
    [Required(ErrorMessage = "請傳入分類")]
    public ExpenseCategoryType? Category { get; set; }

    [Description("原幣")]
    [Required(ErrorMessage = "請傳入原幣")]
    public CurrencyType? Currency { get; set; }

    [Description("原幣金額")]
    public decimal Amount { get; set; }

    [Description("消費日期")]
    [Required(ErrorMessage = "請傳入消費日期")]
    public DateOnly? Date { get; set; }

    [Description("付款人的成員ID")]
    public int PayerId { get; set; }

    [Description("分攤清單")]
    [Required(ErrorMessage = "請傳入分攤清單")]
    public List<ExpenseShareRequest> Shares { get; set; } = [];
}

/// <summary>
/// 花費內容中的一筆分攤
/// </summary>
public class ExpenseShareRequest
{
    [Description("參與者的成員ID")]
    public int MemberId { get; set; }

    [Description("分攤額，以原幣計價")]
    public decimal Amount { get; set; }
}
