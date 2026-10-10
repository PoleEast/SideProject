namespace Project.Shared.Constants;

/// <summary>
/// 資料庫字串欄位的長度上限
/// </summary>
/// <remarks>
/// 修改任一值都會改變資料庫 schema，須 migration。
/// </remarks>
public static class MaxLengths
{
    public const int UserAccount = 32;
    public const int UserName = 32;

    public const int TransactionRemark = 200;

    public const int GroupName = 32;
    public const int GroupDescription = 200;
    public const int GroupInviteCode = 16;

    public const int GroupMemberDisplayName = 32;

    public const int ExpenseName = 50;
    public const int ExpenseDescription = 200;

    public const int ActivityLogSummary = 500;
    public const int ActivityLogActorName = 32;

    public const int ActivityLogDetailText = 100;
}
