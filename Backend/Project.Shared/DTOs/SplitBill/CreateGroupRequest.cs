using Project.Shared.Constants;
using Project.Shared.Types;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

public class CreateGroupRequest
{
    [Description("群組名稱")]
    [Required(ErrorMessage = "請傳入群組名稱")]
    [StringLength(MaxLengths.GroupName, MinimumLength = 1, ErrorMessage = "群組名稱長度請為 {2}~{1} 個字元")]
    public string Name { get; set; } = string.Empty;

    [Description("群組描述")]
    [StringLength(MaxLengths.GroupDescription, ErrorMessage = "群組描述請勿超過 {1} 個字元")]
    public string Description { get; set; } = string.Empty;

    [Description("基準幣")]
    [Required(ErrorMessage = "請傳入基準幣")]
    public CurrencyType? BaseCurrency { get; set; }
}
