using Project.Shared.Constants;
using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

public class UpdateGroupRequest
{
    [Description("群組名稱")]
    [StringLength(MaxLengths.GroupName, MinimumLength = 1, ErrorMessage = "群組名稱長度請為 {2}~{1} 個字元")]
    public string? Name { get; set; }

    [Description("群組描述")]
    [StringLength(MaxLengths.GroupDescription, ErrorMessage = "群組描述請勿超過 {1} 個字元")]
    public string? Description { get; set; }

    [Description("基準幣")]
    public CurrencyType? BaseCurrency { get; set; }
}
