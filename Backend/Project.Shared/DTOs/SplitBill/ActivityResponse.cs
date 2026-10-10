using Project.Shared.Types;
using System.ComponentModel;

namespace Project.Shared.DTOs.SplitBill;

public class ActivityResponse
{
    [Description("資源ID")]
    public int Id { get; set; }

    [Description("變動類型")]
    public ActivityActionType ActionType { get; set; }

    [Description("操作者的名稱")]
    public string ActorName { get; set; } = string.Empty;

    [Description("操作者是否為呼叫者")]
    public bool IsMe { get; set; }

    [Description("敘述，不含主詞")]
    public string Summary { get; set; } = string.Empty;

    [Description("這則動態講的花費ID，與花費無關時為空")]
    public int? TargetExpenseId { get; set; }

    [Description("記錄的時間")]
    public DateTimeOffset CreatedAt { get; set; }

    [Description("動態明細，依成員ID排序")]
    public List<ActivityDetailResponse> Details { get; set; } = [];
}

public class ActivityDetailResponse
{
    [Description("這一列講的成員ID")]
    public int MemberId { get; set; }

    [Description("敘述")]
    public string Text { get; set; } = string.Empty;
}
