using Project.Shared.DTOs.SplitBill;

namespace Project.Api.Helpers;

/// <summary>
/// 結算計算器: 將所有分錄收斂成兩兩淨額
/// </summary>
/// <remarks>
/// 結算採兩兩淨額而非最小化轉帳
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
    /// <returns>淨額為 0 的組合不列入</returns>
    public static List<MemberBalance> CalculateBalances(IEnumerable<BalanceEntry> entries)
    {
        var netByPair = entries
            // 自己欠自己沒有對象可抵銷
            .Where(entry => entry.DebtorMemberId != entry.CreditorMemberId)
            .Select(ToSignedEntry)
            .GroupBy(signed => signed.Pair)
            .Select(group => (Pair: group.Key, Net: group.Sum(signed => signed.Amount)));

        // 依成員 Id 排序，結果才不會隨輸入順序而跳動
        return netByPair
            .Where(pairNet => pairNet.Net != 0)
            .OrderBy(pairNet => pairNet.Pair.LowMemberId)
            .ThenBy(pairNet => pairNet.Pair.HighMemberId)
            .Select(pairNet => ToMemberBalance(pairNet.Pair, pairNet.Net))
            .ToList();
    }

    /// <summary>
    /// 把兩兩淨額四捨五入到基準幣的最小單位
    /// </summary>
    /// <param name="balances">兩兩淨額，須以基準幣計價尚未四捨五入</param>
    /// <param name="decimals">基準幣最小單位的小數位數</param>
    /// <remarks>
    /// 必須明確指定 <see cref="MidpointRounding.AwayFromZero"/>，.NET 預設為銀行家捨入
    /// </remarks>
    /// <returns>四捨五入後仍非零的兩兩淨額；歸 0 的組合不列入</returns>
    public static List<MemberBalance> RoundToUnit(IEnumerable<MemberBalance> balances, int decimals) =>
        balances.Select(balance => balance with { Amount = Math.Round(balance.Amount, decimals, MidpointRounding.AwayFromZero) })
                .Where(balance => balance.Amount != 0)
                .ToList();

    /// <summary>
    /// 把分錄的方向編碼成正負號
    /// </summary>
    /// <returns>鍵固定為 (較小Id, 較大Id)；正值代表較小Id 欠較大Id</returns>
    private static SignedEntry ToSignedEntry(BalanceEntry entry) =>
        entry.DebtorMemberId < entry.CreditorMemberId
            ? new SignedEntry(new MemberPair(entry.DebtorMemberId, entry.CreditorMemberId), entry.Amount)
            : new SignedEntry(new MemberPair(entry.CreditorMemberId, entry.DebtorMemberId), -entry.Amount);

    /// <summary>
    /// <see cref="ToSignedEntry"/> 的反向 - 把正負號解回欠款方向
    /// </summary>
    private static MemberBalance ToMemberBalance(MemberPair pair, decimal net) =>
        net > 0
            ? new MemberBalance(pair.LowMemberId, pair.HighMemberId, net)
            : new MemberBalance(pair.HighMemberId, pair.LowMemberId, -net);

    private readonly record struct MemberPair(int LowMemberId, int HighMemberId);
    private readonly record struct SignedEntry(MemberPair Pair, decimal Amount);
}
