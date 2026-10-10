using Microsoft.EntityFrameworkCore;
using Project.Api.Common;
using Project.Api.Helpers;
using Project.Core.Common;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Globalization;

namespace Project.Api.Services;

/// <summary>
/// 結算服務 - 還款的記錄與刪除，以及從分攤與還款紀錄推導兩兩淨額
/// </summary>
public class SettlementService(ApplicationDbContext dbContext, ILogger<SettlementService> logger)
{
    /// <summary>
    /// 取得群組的結算
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>
    /// 成功時為捨入到基準幣最小單位後的兩兩淨額，捨入後為 0 的組合不列入；
    /// 群組不存在或呼叫者不是成員回 NotFound
    /// </returns>
    public async Task<Result<List<MemberBalance>>> GetBalancesAsync(int groupId, int userId)
    {
        var group = await dbContext.Groups.AsNoTracking()
                                          .AccessibleBy(userId)
                                          .FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<List<MemberBalance>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        return Result<List<MemberBalance>>.Success(await DeriveBalancesAsync(group));
    }

    public async Task<Result<List<SettlementResponse>>> GetSettlementsAsync(int groupId, int userId)
    {
        bool isAccessible = await dbContext.Groups.AccessibleBy(userId).AnyAsync(storedGroup => storedGroup.Id == groupId);

        if (!isAccessible)
        {
            return Result<List<SettlementResponse>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var settlements = await dbContext.Settlements.AsNoTracking()
                                                     .Where(settlement => settlement.GroupId == groupId)
                                                     .OrderByDescending(settlement => settlement.Date)
                                                     .ThenByDescending(settlement => settlement.Id)
                                                     .ToListAsync();

        return Result<List<SettlementResponse>>.Success(settlements.Select(ToResponse).ToList());
    }

    /// <summary>
    /// 記錄一筆還款
    /// </summary>
    /// <remarks>
    /// 刻意不檢查兩人之間的兩兩淨額：先給錢、花費之後才記是正常用法，多還則讓淨額反轉。
    /// 記錯的還款由刪除補救，兩者都會留在群組動態。
    /// </remarks>
    /// <param name="groupId">還款所屬群組的 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <param name="request">還款內容，還款日期須已有值</param>
    /// <returns>
    /// 成功時為記錄下來的還款；群組不存在或呼叫者不是成員回 NotFound；
    /// 內容未通過驗證、或還款人與收款人有人不屬於這個群組回 ValidationError；儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result<SettlementResponse>> RecordSettlementAsync(int groupId, int userId, SettlementRequest request)
    {
        var group = await dbContext.Groups.AsNoTracking()
                                          .AccessibleBy(userId)
                                          .FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<SettlementResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var validation = Validate(request, group.BaseCurrency);

        if (!validation.IsSuccess)
        {
            return Result<SettlementResponse>.Failure(validation);
        }

        var memberNames = await dbContext.GroupMembers.FindDisplayNamesAsync(groupId, [request.FromMemberId, request.ToMemberId]);

        // 兩人不同已經確認過，查回來不足兩筆就是有人不屬於這個群組
        if (memberNames.Count < 2)
        {
            return Result<SettlementResponse>.Failure(ResultCode.ValidationError, "還款人與收款人必須是這個群組的成員");
        }

        var settlement = new Settlement
        {
            GroupId = groupId,
            FromMemberId = request.FromMemberId,
            ToMemberId = request.ToMemberId,
            Amount = request.Amount,
            Date = request.Date!.Value,
            CreatedByUserId = userId
        };

        var recorded = await dbContext.RecordActivityAsync(
            groupId, userId, ActivityActionType.SettlementRecorded, $"記錄了還款：{Describe(settlement, memberNames, group.BaseCurrency)}");

        if (!recorded.IsSuccess)
        {
            return Result<SettlementResponse>.Failure(recorded);
        }

        dbContext.Add(settlement);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "記錄還款失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<SettlementResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<SettlementResponse>.Success(ToResponse(settlement));
    }

    /// <summary>
    /// 刪除還款
    /// </summary>
    /// <remarks>
    /// 刻意不檢查任何淨額，已移除成員的兩兩淨額因此再度非 0 時照常列入結算。
    /// 也不處理並行衝突：還款只有一列，同時刪除的結果與刪除一次相同，代價只有一則重複的動態。
    /// </remarks>
    /// <param name="groupId">還款所屬群組的 ID</param>
    /// <param name="settlementId">要刪除的還款 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>
    /// 刪除完成時為成功；群組不存在、呼叫者不是成員、或找不到還款回 NotFound；儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result> DeleteSettlementAsync(int groupId, int settlementId, int userId)
    {
        var group = await dbContext.Groups.AsNoTracking()
                                          .AccessibleBy(userId)
                                          .FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var settlement = await dbContext.Settlements.FirstOrDefaultAsync(storedSettlement => storedSettlement.Id == settlementId && storedSettlement.GroupId == groupId);

        if (settlement == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此還款");
        }

        var memberNames = await dbContext.GroupMembers.FindDisplayNamesAsync(groupId, [settlement.FromMemberId, settlement.ToMemberId]);

        var recorded = await dbContext.RecordActivityAsync(
            groupId, userId, ActivityActionType.SettlementDeleted, $"刪除了還款：{Describe(settlement, memberNames, group.BaseCurrency)}");

        if (!recorded.IsSuccess)
        {
            return recorded;
        }

        dbContext.Remove(settlement);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "刪除還款失敗。groupId: {GroupId}, settlementId: {SettlementId}, userId: {UserId}", groupId, settlementId, userId);
            return Result.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result.Success();
    }

    /// <summary>
    /// 從分攤與還款紀錄推導群組的兩兩淨額
    /// </summary>
    /// <param name="group">須已通過存取檢查；捨入位數取自它的基準幣</param>
    /// <returns>捨入到基準幣最小單位後的兩兩淨額；捨入後為 0 的組合不列入</returns>
    public async Task<List<MemberBalance>> DeriveBalancesAsync(Group group)
    {
        // 參與者欠付款人「分攤額 × Rate」
        var shareEntries = await dbContext.ExpenseShares
            .Where(share => share.Expense.GroupId == group.Id)
            .Select(share => new BalanceEntry(share.GroupMemberId, share.Expense.PayerId, share.Amount * share.Expense.Rate))
            .ToListAsync();

        // 還款以反向分錄表達：收款人等同欠了還款人
        var repaymentEntries = await dbContext.Settlements
            .Where(settlement => settlement.GroupId == group.Id)
            .Select(settlement => new BalanceEntry(settlement.ToMemberId, settlement.FromMemberId, settlement.Amount))
            .ToListAsync();

        var balances = SettlementCalculator.CalculateBalances([.. shareEntries, .. repaymentEntries]);

        return SettlementCalculator.RoundToUnit(balances, group.BaseCurrency.MinorUnitDecimals());
    }

    /// <summary>
    /// 驗證還款內容中不必查詢資料庫的規則
    /// </summary>
    /// <param name="request">要驗證的還款內容，還款日期須已有值</param>
    /// <param name="baseCurrency">還款所屬群組的基準幣</param>
    /// <returns>全部通過時為成功；否則回 ValidationError，訊息是第一條不通過的規則</returns>
    private static Result Validate(SettlementRequest request, CurrencyType baseCurrency)
    {
        int minorUnitDecimals = baseCurrency.MinorUnitDecimals();

        if (request.FromMemberId == request.ToMemberId)
        {
            return Result.Failure(ResultCode.ValidationError, "還款人與收款人不可為同一人");
        }

        if (!AmountRules.IsInRange(request.Amount))
        {
            return Result.Failure(ResultCode.ValidationError, $"金額須大於 0，且不超過 {AmountRules.Max.ToString("N2", CultureInfo.InvariantCulture)}");
        }

        if (!AmountRules.FitsMinorUnit(request.Amount, baseCurrency))
        {
            return Result.Failure(ResultCode.ValidationError, minorUnitDecimals == 0
                ? $"{baseCurrency} 的金額必須是整數"
                : $"{baseCurrency} 的金額最多 {minorUnitDecimals} 位小數");
        }

        if (!CalendarDateRules.IsNotInFuture(request.Date!.Value))
        {
            return Result.Failure(ResultCode.ValidationError, "還款日期不可為未來的日期");
        }

        return Result.Success();
    }

    /// <summary>
    /// 組出動態敘述中說明是哪一筆還款的那一段
    /// </summary>
    /// <remarks>
    /// 還款沒有名稱，還款日期是分辨同樣兩人之間多筆同額還款的唯一依據。
    /// </remarks>
    /// <param name="settlement">要敘述的還款</param>
    /// <param name="memberNames">成員 ID 對應顯示名稱的字典，須含還款人與收款人</param>
    /// <param name="baseCurrency">還款所屬群組的基準幣</param>
    /// <returns>誰還給誰、多少錢、還款日期是哪一天</returns>
    private static string Describe(Settlement settlement, Dictionary<int, string> memberNames, CurrencyType baseCurrency)
        => $"「{memberNames[settlement.FromMemberId]}」還給「{memberNames[settlement.ToMemberId]}」"
            + $"{StringFormatter.FormatMoney(baseCurrency, settlement.Amount)}，還款日期 {StringFormatter.FormatDate(settlement.Date)}";

    private static SettlementResponse ToResponse(Settlement settlement) => new()
    {
        Id = settlement.Id,
        FromMemberId = settlement.FromMemberId,
        ToMemberId = settlement.ToMemberId,
        Amount = settlement.Amount,
        Date = settlement.Date,
        CreatedAt = settlement.CreatedAt
    };
}
