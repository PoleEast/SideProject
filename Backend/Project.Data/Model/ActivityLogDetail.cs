namespace Project.Data.Model;

/// <summary>
/// 動態明細 - 一則群組動態底下的補充敘述，一列一句
/// </summary>
public class ActivityLogDetail
{
    public int Id { get; set; }
    public int ActivityLogId { get; set; }

    /// <summary>
    /// 這一列講的是哪一位成員
    /// </summary>
    public int GroupMemberId { get; set; }

    public string Text { get; set; } = string.Empty;

    public ActivityLog ActivityLog { get; set; } = null!;
}
