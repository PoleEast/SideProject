using Project.Api.Helpers;
using Project.Shared.DTOs.SplitBill;

namespace Project.Tests;

/// <summary>
/// SettlementCalculator 單元測試
/// 測試兩兩淨額的相抵、還款扣抵與溢還處理
/// </summary>
public class SettlementCalculatorTests
{
    /// <summary>
    /// 將一筆還款表達成反向分錄 - 收錢的一方等同欠了還錢的一方
    /// </summary>
    private static BalanceEntry AsRepayment(int fromMemberId, int toMemberId, decimal amount) =>
        new(DebtorMemberId: toMemberId, CreditorMemberId: fromMemberId, Amount: amount);

    /// <summary>
    /// 從結果中取出指定兩人之間的淨額，方向不拘。
    /// 回傳正值代表 debtor 欠 creditor，負值代表相反。找不到則為 0。
    /// </summary>
    private static decimal NetBetween(IEnumerable<MemberBalance> balances, int debtorId, int creditorId)
    {
        foreach (var balance in balances)
        {
            if (balance.DebtorMemberId == debtorId && balance.CreditorMemberId == creditorId) return balance.Amount;
            if (balance.DebtorMemberId == creditorId && balance.CreditorMemberId == debtorId) return -balance.Amount;
        }

        return 0;
    }

    /// <summary>
    /// 算出每位成員的淨收支（應付為負、應收為正），依 Id 排序，淨額為 0 者不列入。
    /// 分錄與淨額都能餵進來，兩邊的結果必須相同 —— 相抵不會改變任何人的淨收支。
    /// </summary>
    private static List<KeyValuePair<int, decimal>> NetByMember(
        IEnumerable<(int DebtorMemberId, int CreditorMemberId, decimal Amount)> items)
    {
        var netByMember = new Dictionary<int, decimal>();

        foreach (var (debtorMemberId, creditorMemberId, amount) in items)
        {
            netByMember.TryGetValue(debtorMemberId, out var debtorNet);
            netByMember[debtorMemberId] = debtorNet - amount;

            netByMember.TryGetValue(creditorMemberId, out var creditorNet);
            netByMember[creditorMemberId] = creditorNet + amount;
        }

        return netByMember
            .Where(pair => pair.Value != 0)
            .OrderBy(pair => pair.Key)
            .ToList();
    }

    #region 基本相抵

    [Fact(DisplayName = "無任何分錄，回傳空清單")]
    public void CalculateBalances_NoDebts_ReturnsEmpty()
    {
        // Act
        var result = SettlementCalculator.CalculateBalances([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "單筆花費：付款人以外的參與者各欠付款人一份")]
    public void CalculateBalances_SingleExpense_EachParticipantOwesPayer()
    {
        // Arrange - 成員 1 墊付 300，三人各分攤 100（成員 1 自己那份不構成分錄）
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 100m),
            new(DebtorMemberId: 3, CreditorMemberId: 1, Amount: 100m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100m, NetBetween(result, 2, 1));
        Assert.Equal(100m, NetBetween(result, 3, 1));
    }

    [Fact(DisplayName = "同一對成員的多筆同向分錄會累加")]
    public void CalculateBalances_SameDirectionDebts_AreAccumulated()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 100m),
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 250m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(2, balance.DebtorMemberId);
        Assert.Equal(1, balance.CreditorMemberId);
        Assert.Equal(350m, balance.Amount);
    }

    [Fact(DisplayName = "反向分錄互相抵銷，只留下淨額與正確方向")]
    public void CalculateBalances_OppositeDebts_AreNetted()
    {
        // Arrange - 2 欠 1 三百，1 欠 2 一百 → 淨額 2 欠 1 兩百
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 100m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(2, balance.DebtorMemberId);
        Assert.Equal(1, balance.CreditorMemberId);
        Assert.Equal(200m, balance.Amount);
    }

    [Fact(DisplayName = "反向分錄完全抵銷，該組合不出現在結果中")]
    public void CalculateBalances_FullyOffsetDebts_AreExcluded()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 300m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "抵銷後為零的組合被排除，其他組合仍保留")]
    public void CalculateBalances_MixedOffsets_KeepsOnlyNonZero()
    {
        // Arrange - 1 與 2 完全抵銷，3 仍欠 1
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 300m),
            new(DebtorMemberId: 3, CreditorMemberId: 1, Amount: 50m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(3, balance.DebtorMemberId);
        Assert.Equal(1, balance.CreditorMemberId);
        Assert.Equal(50m, balance.Amount);
    }

    [Fact(DisplayName = "同一人對自己的分錄被忽略")]
    public void CalculateBalances_SelfEntry_IsIgnored()
    {
        // Arrange - 付款人自己那份分攤不該產生分錄，真的傳進來也不構成任何淨額
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 1, CreditorMemberId: 1, Amount: 100m),
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 100m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(2, balance.DebtorMemberId);
        Assert.Equal(1, balance.CreditorMemberId);
        Assert.Equal(100m, balance.Amount);
    }

    #endregion

    #region 還款情境（以反向分錄表達）

    [Fact(DisplayName = "部分還款：淨額扣掉已還金額")]
    public void CalculateBalances_PartialRepayment_ReducesBalance()
    {
        // Arrange - 2 欠 1 三百，已還 120
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 120m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(2, balance.DebtorMemberId);
        Assert.Equal(1, balance.CreditorMemberId);
        Assert.Equal(180m, balance.Amount);
    }

    [Fact(DisplayName = "還清：該組合不出現在結果中")]
    public void CalculateBalances_FullRepayment_ExcludesPair()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 300m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "多筆還款會累加後一起扣抵")]
    public void CalculateBalances_MultipleRepayments_AreAccumulated()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 100m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 50m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(150m, balance.Amount);
    }

    [Fact(DisplayName = "溢還：淨額轉向，改由對方欠款")]
    public void CalculateBalances_OverRepayment_ReversesDirection()
    {
        // Arrange - 2 欠 1 三百，卻還了 500 → 1 反過來欠 2 兩百
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 500m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(1, balance.DebtorMemberId);
        Assert.Equal(2, balance.CreditorMemberId);
        Assert.Equal(200m, balance.Amount);
    }

    [Fact(DisplayName = "無對應欠款的還款，直接形成反向欠款")]
    public void CalculateBalances_RepaymentWithoutDebt_CreatesReverseBalance()
    {
        // Arrange - 2 沒欠 1 任何錢卻還了 80 → 1 欠 2 八十
        BalanceEntry[] entries = [AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 80m)];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(1, balance.DebtorMemberId);
        Assert.Equal(2, balance.CreditorMemberId);
        Assert.Equal(80m, balance.Amount);
    }

    #endregion

    #region 不變量

    [Fact(DisplayName = "不變量：每位成員的淨收支在相抵前後一致")]
    public void CalculateBalances_AnyInput_PreservesPerMemberNet()
    {
        // Arrange - 四人多筆交錯分錄與還款
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 333.33m),
            new(DebtorMemberId: 3, CreditorMemberId: 1, Amount: 333.33m),
            new(DebtorMemberId: 4, CreditorMemberId: 1, Amount: 333.34m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 120.50m),
            new(DebtorMemberId: 3, CreditorMemberId: 2, Amount: 120.50m),
            new(DebtorMemberId: 4, CreditorMemberId: 3, Amount: 75m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 200m),
            AsRepayment(fromMemberId: 4, toMemberId: 1, amount: 500m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert - 相抵前後，每個人的淨收支都不能改變
        var expected = NetByMember(entries.Select(entry =>
            (entry.DebtorMemberId, entry.CreditorMemberId, entry.Amount)));
        var actual = NetByMember(result.Select(balance =>
            (balance.DebtorMemberId, balance.CreditorMemberId, balance.Amount)));

        Assert.Equal(expected, actual);
    }

    [Fact(DisplayName = "不變量：結果順序不受輸入順序影響")]
    public void CalculateBalances_ReorderedInput_ProducesSameOrder()
    {
        // Arrange - 同一組分錄，兩種輸入順序
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 3, CreditorMemberId: 1, Amount: 50m),
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            new(DebtorMemberId: 3, CreditorMemberId: 2, Amount: 80m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);
        var reorderedResult = SettlementCalculator.CalculateBalances(entries.Reverse().ToArray());

        // Assert
        Assert.Equal(result, reorderedResult);
    }

    [Fact(DisplayName = "不變量：每一對成員在結果中最多出現一次")]
    public void CalculateBalances_AnyInput_EachPairAppearsAtMostOnce()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 100m),
            new(DebtorMemberId: 3, CreditorMemberId: 1, Amount: 50m),
            new(DebtorMemberId: 1, CreditorMemberId: 3, Amount: 20m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var pairs = result
            .Select(balance => (
                Low: Math.Min(balance.DebtorMemberId, balance.CreditorMemberId),
                High: Math.Max(balance.DebtorMemberId, balance.CreditorMemberId)))
            .ToList();

        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Fact(DisplayName = "不變量：回傳的金額恆為正")]
    public void CalculateBalances_AnyInput_AmountsArePositive()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 300m),
            AsRepayment(fromMemberId: 2, toMemberId: 1, amount: 500m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        Assert.All(result, balance => Assert.True(balance.Amount > 0));
    }

    [Fact(DisplayName = "小數金額不因相抵而失去精度")]
    public void CalculateBalances_DecimalAmounts_PreservePrecision()
    {
        // Arrange
        BalanceEntry[] entries =
        [
            new(DebtorMemberId: 2, CreditorMemberId: 1, Amount: 33.33m),
            new(DebtorMemberId: 1, CreditorMemberId: 2, Amount: 11.11m),
        ];

        // Act
        var result = SettlementCalculator.CalculateBalances(entries);

        // Assert
        var balance = Assert.Single(result);
        Assert.Equal(22.22m, balance.Amount);
    }

    #endregion
}
