using Microsoft.EntityFrameworkCore;
using Project.Api.Helpers;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;

namespace Project.Api.Services;

/// <summary>
/// 結算服務 - 從分攤與還款紀錄推導兩兩淨額
/// </summary>
public class SettlementService(ApplicationDbContext dbContext)
{
    /// <summary>
    /// 取得群組捨入到基準幣最小單位後的兩兩淨額
    /// </summary>
    /// <param name="group">須已通過存取檢查；捨入位數取自它的基準幣</param>
    public async Task<List<MemberBalance>> GetBalancesAsync(Group group)
    {
        // 參與者欠付款人「分攤額 × Rate」
        var shareEntries = await dbContext.ExpenseShares
            .Where(share => share.Expense.GroupId == group.Id)
            .Select(share => new BalanceEntry(share.GroupMemberId, share.Expense.PayerId, share.Amount * share.Expense.Rate))
            .ToListAsync();

        // 還款以反向分錄表達：收錢的一方等同欠了還錢的一方
        var repaymentEntries = await dbContext.Settlements
            .Where(settlement => settlement.GroupId == group.Id)
            .Select(settlement => new BalanceEntry(settlement.ToMemberId, settlement.FromMemberId, settlement.Amount))
            .ToListAsync();

        var balances = SettlementCalculator.CalculateBalances([.. shareEntries, .. repaymentEntries]);

        return SettlementCalculator.RoundToUnit(balances, group.BaseCurrency.MinorUnitDecimals());
    }
}
