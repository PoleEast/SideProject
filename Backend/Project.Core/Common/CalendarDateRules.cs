namespace Project.Core.Common;

/// <summary>
/// 使用者填寫的日曆日的驗證規則
/// </summary>
public static class CalendarDateRules
{
    /// <summary>
    /// 判斷日曆日是否不在未來
    /// </summary>
    /// <remarks>
    /// 放寬到 UTC 的次日：日曆日是使用者當地的日期，比 UTC 快的時區在當地凌晨填今天，就是 UTC 的明天。
    /// </remarks>
    /// <param name="date">要檢查的日曆日</param>
    /// <returns>不晚於 UTC 的次日時為 true</returns>
    public static bool IsNotInFuture(DateOnly date) => date <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
}
