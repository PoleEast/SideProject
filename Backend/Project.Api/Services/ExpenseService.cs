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

public class ExpenseService(ApplicationDbContext dbContext, ExchangeRateService exchangeRateService, ILogger<ExpenseService> logger)
{
    private const string ConcurrentChangeMessage = "這筆花費剛被其他人修改，請重新整理後再試";

    /// <summary>
    /// Rate 存入前捨入到的小數位數
    /// </summary>
    /// <remarks>
    /// 須與 Rate 欄位的精度相同。
    /// </remarks>
    private const int RateDecimals = 6;

    public async Task<Result<List<ExpenseResponse>>> GetExpensesAsync(int groupId, int userId)
    {
        bool isAccessible = await dbContext.Groups.AccessibleBy(userId).AnyAsync(storedGroup => storedGroup.Id == groupId);

        if (!isAccessible)
        {
            return Result<List<ExpenseResponse>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var expenses = await dbContext.Expenses.AsNoTracking()
                                               .Include(expense => expense.ExpenseShares)
                                               .Where(expense => expense.GroupId == groupId)
                                               .OrderByDescending(expense => expense.Date)
                                               .ThenByDescending(expense => expense.Id)
                                               .ToListAsync();

        return Result<List<ExpenseResponse>>.Success(expenses.Select(ToResponse).ToList());
    }

    public async Task<Result<ExpenseResponse>> CreateExpenseAsync(int groupId, int userId, ExpenseRequest request)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<ExpenseResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var validation = await ValidateAsync(groupId, request, existingExpense: null);

        if (!validation.IsSuccess)
        {
            return Result<ExpenseResponse>.Failure(validation);
        }

        var resolvedRate = await ResolveRateAsync(request.Currency!.Value, group.BaseCurrency);

        if (!resolvedRate.IsSuccess)
        {
            return Result<ExpenseResponse>.Failure(resolvedRate);
        }

        var expense = new Expense
        {
            GroupId = groupId,
            PayerId = request.PayerId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Category = request.Category!.Value,
            Currency = request.Currency.Value,
            Amount = request.Amount,
            Rate = resolvedRate.Value,
            Date = request.Date!.Value,
            CreatedByUserId = userId,
            ExpenseShares = ToShares(request.Shares)
        };

        var memberNames = await dbContext.GroupMembers.FindDisplayNamesAsync(groupId, [expense.PayerId]);

        dbContext.Add(expense);

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.ExpenseCreated,
            TargetExpense = expense,
            Summary = $"新增了花費「{expense.Name}」{StringFormatter.FormatMoney(expense.Currency, expense.Amount)}，"
                + $"由「{memberNames[expense.PayerId]}」付款，{expense.ExpenseShares.Count} 人分攤"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "建立花費失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<ExpenseResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<ExpenseResponse>.Success(ToResponse(expense));
    }

    /// <summary>
    /// 以請求的內容覆蓋整筆花費
    /// </summary>
    /// <param name="groupId">花費所屬群組的 ID</param>
    /// <param name="expenseId">要編輯的花費 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <param name="request">新的花費內容，必填欄位須已有值</param>
    /// <returns>
    /// 成功時為編輯後的花費，內容與現況相同時不寫入；群組不存在、呼叫者不是成員、或找不到花費回 NotFound；
    /// 內容未通過驗證回 ValidationError；原幣有變更但查不到匯率回 ExternalApiError；
    /// 花費同時被其他人修改回 Conflict；儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result<ExpenseResponse>> UpdateExpenseAsync(int groupId, int expenseId, int userId, ExpenseRequest request)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<ExpenseResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var expense = await dbContext.Expenses.Include(storedExpense => storedExpense.ExpenseShares).FirstOrDefaultAsync(storedExpense => storedExpense.Id == expenseId && storedExpense.GroupId == groupId);

        if (expense == null)
        {
            return Result<ExpenseResponse>.Failure(ResultCode.NotFound, "找不到此花費");
        }

        var validation = await ValidateAsync(groupId, request, expense);

        if (!validation.IsSuccess)
        {
            return Result<ExpenseResponse>.Failure(validation);
        }

        var currency = request.Currency!.Value;
        string? shareChange = DescribeShareChange(expense.ExpenseShares, request.Shares);
        string? summary = await BuildUpdateSummaryAsync(groupId, expense, request, shareChange);

        if (summary == null)
        {
            return Result<ExpenseResponse>.Success(ToResponse(expense));
        }

        // Rate 跟著原幣走：原幣不變就不動，改了才重新取得。取得失敗時尚未改動任何資料
        decimal rate = expense.Rate;

        if (currency != expense.Currency)
        {
            var resolvedRate = await ResolveRateAsync(currency, group.BaseCurrency);

            if (!resolvedRate.IsSuccess)
            {
                return Result<ExpenseResponse>.Failure(resolvedRate);
            }

            rate = resolvedRate.Value;
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.ExpenseUpdated,
            TargetExpenseId = expense.Id,
            Summary = summary
        });

        expense.Name = request.Name.Trim();
        expense.Description = request.Description;
        expense.Category = request.Category!.Value;
        expense.Currency = currency;
        expense.Amount = request.Amount;
        expense.Rate = rate;
        expense.Date = request.Date!.Value;
        expense.PayerId = request.PayerId;

        if (shareChange != null)
        {
            // 存入的必須就是送來的那一組，因此整批換掉，不逐筆比對更新
            dbContext.RemoveRange(expense.ExpenseShares);

            foreach (var share in ToShares(request.Shares))
            {
                expense.ExpenseShares.Add(share);
            }
        }

        // 只改分攤時主表沒有任何欄位變動，不推進 UpdatedAt 就不會檢查 concurrency token
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ExpenseResponse>.Failure(ResultCode.Conflict, ConcurrentChangeMessage);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "編輯花費失敗。groupId: {GroupId}, expenseId: {ExpenseId}, userId: {UserId}", groupId, expenseId, userId);
            return Result<ExpenseResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<ExpenseResponse>.Success(ToResponse(expense));
    }

    /// <summary>
    /// 刪除花費
    /// </summary>
    /// <remarks>
    /// 分攤無須逐一軟刪，query filter 已串接花費的軟刪欄位。
    /// </remarks>
    /// <param name="groupId">花費所屬群組的 ID</param>
    /// <param name="expenseId">要刪除的花費 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>
    /// 刪除完成時為成功；群組不存在、呼叫者不是成員、或找不到花費回 NotFound；
    /// 花費同時被其他人修改回 Conflict；儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result> DeleteExpenseAsync(int groupId, int expenseId, int userId)
    {
        bool isAccessible = await dbContext.Groups.AccessibleBy(userId).AnyAsync(storedGroup => storedGroup.Id == groupId);

        if (!isAccessible)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var expense = await dbContext.Expenses.FirstOrDefaultAsync(storedExpense => storedExpense.Id == expenseId && storedExpense.GroupId == groupId);

        if (expense == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此花費");
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.ExpenseDeleted,
            TargetExpenseId = expense.Id,
            Summary = $"刪除了花費「{expense.Name}」{StringFormatter.FormatMoney(expense.Currency, expense.Amount)}"
        });

        // 軟刪不會推進 UpdatedAt；不手動推進的話，同時進行的編輯拿著舊值仍能通過 concurrency token 的檢查
        expense.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Remove(expense);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ResultCode.Conflict, ConcurrentChangeMessage);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "刪除花費失敗。groupId: {GroupId}, expenseId: {ExpenseId}, userId: {UserId}", groupId, expenseId, userId);
            return Result.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result.Success();
    }

    /// <summary>
    /// 驗證花費內容
    /// </summary>
    /// <param name="groupId">花費所屬群組的 ID</param>
    /// <param name="request">要驗證的花費內容，必填欄位須已有值</param>
    /// <param name="existingExpense">這份內容要取代的既有花費，須已載入分攤；內容屬於新花費時為 null</param>
    /// <returns>全部通過時為成功；否則回 ValidationError，訊息是第一條不通過的規則</returns>
    private async Task<Result> ValidateAsync(int groupId, ExpenseRequest request, Expense? existingExpense)
    {
        var currency = request.Currency!.Value;
        int minorUnitDecimals = currency.MinorUnitDecimals();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure(ResultCode.ValidationError, "請輸入花費名稱");
        }

        if (!AmountRules.IsInRange(request.Amount))
        {
            return Result.Failure(ResultCode.ValidationError, $"金額須大於 0，且不超過 {AmountRules.Max.ToString("N2", CultureInfo.InvariantCulture)}");
        }

        if (!CalendarDateRules.IsNotInFuture(request.Date!.Value))
        {
            return Result.Failure(ResultCode.ValidationError, "消費日期不可為未來的日期");
        }

        if (request.Shares.Count == 0)
        {
            return Result.Failure(ResultCode.ValidationError, "請至少選擇一位參與者");
        }

        if (request.Shares.DistinctBy(share => share.MemberId).Count() != request.Shares.Count)
        {
            return Result.Failure(ResultCode.ValidationError, "參與者不可重複");
        }

        if (request.Shares.Any(share => share.Amount <= 0))
        {
            return Result.Failure(ResultCode.ValidationError, "每位參與者的分攤必須大於 0");
        }

        // 加總相等不代表每一筆都付得出去，金額與每一筆分攤都要檢查
        var amounts = request.Shares.Select(share => share.Amount).Append(request.Amount);

        if (!amounts.All(amount => AmountRules.FitsMinorUnit(amount, currency)))
        {
            return Result.Failure(ResultCode.ValidationError, minorUnitDecimals == 0
                ? $"{currency} 的金額與分攤必須是整數"
                : $"{currency} 的金額與分攤最多 {minorUnitDecimals} 位小數");
        }

        if (request.Shares.Sum(share => share.Amount) != request.Amount)
        {
            return Result.Failure(ResultCode.ValidationError, "分攤加總必須等於花費金額");
        }

        // 只有這一條需要查資料庫，放在最後。
        var eligibleMemberIds = await FindEligibleMemberIdsAsync(groupId, existingExpense);
        var involvedMemberIds = request.Shares.Select(share => share.MemberId).Append(request.PayerId);

        if (!involvedMemberIds.All(eligibleMemberIds.Contains))
        {
            return Result.Failure(ResultCode.ValidationError, "付款人與參與者必須是這個群組的現役成員");
        }

        return Result.Success();
    }

    /// <summary>
    /// 找出可以擔任付款人或參與者的成員
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    /// <param name="existingExpense">既有的花費，須已載入分攤；沒有時為 null</param>
    /// <returns>群組現役成員的 ID，加上 <paramref name="existingExpense"/> 原本的付款人與參與者的 ID（即使他們已被移除）</returns>
    private async Task<HashSet<int>> FindEligibleMemberIdsAsync(int groupId, Expense? existingExpense)
    {
        var eligibleMemberIds = await dbContext.GroupMembers.Where(member => member.GroupId == groupId)
                                                            .Select(member => member.Id)
                                                            .ToHashSetAsync();

        if (existingExpense != null)
        {
            eligibleMemberIds.Add(existingExpense.PayerId);
            eligibleMemberIds.UnionWith(existingExpense.ExpenseShares.Select(share => share.GroupMemberId));
        }

        return eligibleMemberIds;
    }

    /// <summary>
    /// 取得「原幣 → 基準幣」的 Rate
    /// </summary>
    /// <param name="currency">原幣</param>
    /// <param name="baseCurrency">基準幣</param>
    /// <returns>
    /// 成功時為捨入到 <see cref="RateDecimals"/> 位小數的 Rate，原幣等於基準幣時為 1 且不查詢匯率；
    /// 查不到匯率時回 ExternalApiError
    /// </returns>
    private async Task<Result<decimal>> ResolveRateAsync(CurrencyType currency, CurrencyType baseCurrency)
    {
        if (currency == baseCurrency)
        {
            return Result<decimal>.Success(1m);
        }

        var exchangeRate = await exchangeRateService.GetExchangeRateAsync(currency);

        if (!exchangeRate.IsSuccess
            || exchangeRate.Value == null
            || !exchangeRate.Value.ConversionRates.TryGetValue(baseCurrency, out decimal rate))
        {
            return Result<decimal>.Failure(ResultCode.ExternalApiError, "目前無法取得匯率，請稍後再試");
        }

        return Result<decimal>.Success(Math.Round(rate, RateDecimals, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// 比較花費的現況與請求，組出編輯花費的動態敘述
    /// </summary>
    /// <param name="groupId">花費所屬群組的 ID</param>
    /// <param name="expense">花費的現況</param>
    /// <param name="request">新的花費內容，必填欄位須已有值</param>
    /// <param name="shareChange">描述分攤變動的那一句，分攤沒有變動時為 null</param>
    /// <returns>只寫有變動的欄位，順序固定；內容與現況完全相同時為 null</returns>
    private async Task<string?> BuildUpdateSummaryAsync(int groupId, Expense expense, ExpenseRequest request, string? shareChange)
    {
        string name = request.Name.Trim();
        var category = request.Category!.Value;
        var currency = request.Currency!.Value;
        var date = request.Date!.Value;

        List<string> changes = [];

        if (name != expense.Name)
        {
            changes.Add($"名稱改為「{name}」");
        }

        // 金額與原幣合成一個值比較，任一個變了就寫成一句
        if (currency != expense.Currency || request.Amount != expense.Amount)
        {
            changes.Add($"金額由 {StringFormatter.FormatMoney(expense.Currency, expense.Amount)} 改為 {StringFormatter.FormatMoney(currency, request.Amount)}");
        }

        if (request.PayerId != expense.PayerId)
        {
            var memberNames = await dbContext.GroupMembers.FindDisplayNamesAsync(groupId, [expense.PayerId, request.PayerId]);
            changes.Add($"付款人由「{memberNames[expense.PayerId]}」改為「{memberNames[request.PayerId]}」");
        }

        if (category != expense.Category)
        {
            changes.Add($"分類由{expense.Category.GetDescription()}改為{category.GetDescription()}");
        }

        if (date != expense.Date)
        {
            changes.Add($"消費日期由 {StringFormatter.FormatDate(expense.Date)} 改為 {StringFormatter.FormatDate(date)}");
        }

        if (shareChange != null)
        {
            changes.Add(shareChange);
        }

        if (request.Description != expense.Description)
        {
            changes.Add("描述");
        }

        return changes.Count == 0
            ? null
            : $"修改了花費「{expense.Name}」：{string.Join("、", changes)}";
    }

    /// <summary>
    /// 比較編輯前後的分攤，組出動態敘述中描述分攤變動的那一句
    /// </summary>
    /// <remarks>
    /// 只寫發生了什麼，不逐人列出前後值，敘述的長度不可隨參與者人數成長。
    /// </remarks>
    /// <param name="currentShares">編輯前的分攤</param>
    /// <param name="requestedShares">編輯後的分攤</param>
    /// <returns>描述變動的一句話；分攤沒有變動時為 null</returns>
    private static string? DescribeShareChange(IEnumerable<ExpenseShare> currentShares, IEnumerable<ExpenseShareRequest> requestedShares)
    {
        var before = currentShares.ToDictionary(share => share.GroupMemberId, share => share.Amount);
        var after = requestedShares.ToDictionary(share => share.MemberId, share => share.Amount);

        if (before.Count != after.Count)
        {
            return $"參與者由 {before.Count} 人改為 {after.Count} 人";
        }

        if (!before.Keys.All(after.ContainsKey))
        {
            return "參與者有更動";
        }

        if (before.Any(share => after[share.Key] != share.Value))
        {
            return "分攤金額有調整";
        }

        return null;
    }

    private static List<ExpenseShare> ToShares(IEnumerable<ExpenseShareRequest> shares)
        => shares.Select(share => new ExpenseShare { GroupMemberId = share.MemberId, Amount = share.Amount }).ToList();

    private static ExpenseResponse ToResponse(Expense expense) => new()
    {
        Id = expense.Id,
        Name = expense.Name,
        Description = expense.Description,
        Category = expense.Category,
        Currency = expense.Currency,
        Amount = expense.Amount,
        Rate = expense.Rate,
        Date = expense.Date,
        PayerId = expense.PayerId,
        // 編輯存檔後，被作廢的舊分攤軟刪了仍留在已追蹤的集合裡
        Shares = expense.ExpenseShares.Where(share => share.DeletedAt == null)
                                      .OrderBy(share => share.GroupMemberId)
                                      .Select(share => new ExpenseShareResponse { MemberId = share.GroupMemberId, Amount = share.Amount })
                                      .ToList(),
        CreatedAt = expense.CreatedAt
    };
}
