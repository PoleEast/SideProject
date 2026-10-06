using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project.Shared.DTOs.SplitBill;

public class AddGroupMembersRequest
{
    [Description("顯示名稱清單")]
    [Required(ErrorMessage = "請傳入顯示名稱清單")]
    public List<string> DisplayNames { get; set; } = [];
}
