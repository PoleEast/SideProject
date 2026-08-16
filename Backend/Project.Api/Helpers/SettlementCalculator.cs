using Project.Shared.DTOs.SplitBill;

namespace Project.Api.Helpers;

/// <summary>
/// 結算計算器 - 將所有分錄收斂成兩兩淨額
/// </summary>
/// <remarks>
/// 只吃純數字，不接觸匯率 API 與 DB（見 ADR 20260731_SplitBill匯率於建立花費時鎖入）。
/// 結算採兩兩淨額而非最小化轉帳（見 ADR 20260731_結算採兩兩淨額而非最小化轉帳）。
/// </remarks>
public static class SettlementCalculator
{
    /// <summary>
    /// 計算所有成員之間互相抵銷後的淨額
    /// </summary>
    /// <remarks>
    /// 不變量：所有淨額的收支加總必為 0（每一筆金額都同時是某人的應付與另一人的應收）。
    /// </remarks>
    /// <param name="entries">分錄，須以基準幣計價。已還款以反向分錄表達</param>
    /// <returns>所有非零的兩兩淨額，<c>Amount</c> 恆為正；淨額為 0 的組合不列入</returns>
    public static List<MemberBalance> CalculateBalances(IEnumerable<BalanceEntry> entries)
    {
        // 以 (較小Id, 較大Id) 為鍵分組，方向改由金額正負表達，
        // 正值代表較小Id 欠較大Id，反向分錄記為負值後相加即自然相抵
        var netByPair = entries
            .Where(entry => entry.DebtorMemberId != entry.CreditorMemberId)
            .GroupBy(entry => (
                LowMemberId: Math.Min(entry.DebtorMemberId, entry.CreditorMemberId),
                HighMemberId: Math.Max(entry.DebtorMemberId, entry.CreditorMemberId)))
            .Select(group => (
                group.Key,
                Net: group.Sum(entry =>
                    entry.DebtorMemberId == group.Key.LowMemberId ? entry.Amount : -entry.Amount)));

        // 依成員 Id 排序，結果才不會隨輸入順序（即 DB 回傳的列順序）而跳動
        return netByPair
            .Where(pair => pair.Net != 0)
            .OrderBy(pair => pair.Key.LowMemberId)
            .ThenBy(pair => pair.Key.HighMemberId)
            .Select(pair => pair.Net > 0
                ? new MemberBalance(pair.Key.LowMemberId, pair.Key.HighMemberId, pair.Net)
                : new MemberBalance(pair.Key.HighMemberId, pair.Key.LowMemberId, -pair.Net))
            .ToList();
    }
}
