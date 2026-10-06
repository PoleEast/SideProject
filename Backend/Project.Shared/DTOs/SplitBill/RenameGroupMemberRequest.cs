using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

public class RenameGroupMemberRequest
{
    [Description("顯示名稱")]
    [Required(ErrorMessage = "請傳入顯示名稱")]
    public string DisplayName { get; set; } = string.Empty;
}
