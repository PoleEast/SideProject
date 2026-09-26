using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Api.Common;
using Project.Api.Helpers;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;

namespace Project.Api.Services;

public class GroupService(ApplicationDbContext dbContext, ILogger<GroupService> logger)
{
    /// <summary>
    /// 建立群組，並將建立者加入為第一位成員
    /// </summary>
    public async Task<Result<GroupResponse>> CreateGroupAsync(int userId, CreateGroupRequest request)
    {
        string? userName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Name)
            .FirstOrDefaultAsync();

        if (userName == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.Unauthorized, "使用者不存在");
        }

        var group = new Group
        {
            OwnerUserId = userId,
            Name = request.Name,
            Description = request.Description,
            BaseCurrency = request.BaseCurrency!.Value,
            InviteCode = InviteCodeGenerator.Generate(),

            // 擁有者自動成為成員，顯示名稱取 User.Name。
            // 之後 User 改名不同步。
            GroupMembers = [new GroupMember { UserId = userId, DisplayName = userName }]
        };

        dbContext.Add(group);

        dbContext.Add(new ActivityLog
        {
            Group = group,
            ActorUserId = userId,
            ActionType = ActivityActionType.GroupCreated,
            Summary = $"建立了群組「{request.Name}」，基準幣為 {request.BaseCurrency}"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "建立群組失敗。userId: {UserId}", userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 取得使用者參與的所有群組
    /// </summary>
    public async Task<Result<List<GroupResponse>>> GetMyGroupsAsync(int userId)
    {
        var groups = await dbContext.Groups.AccessibleBy(userId).ToListAsync();

        return Result<List<GroupResponse>>.Success(groups.Adapt<List<GroupResponse>>());
    }

    /// <summary>
    /// 取得單一群組
    /// </summary>
    public async Task<Result<GroupResponse>> GetGroupByIdAsync(int groupId, int userId)
    {
        var group = await FindAccessibleGroupAsync(groupId, userId);

        if (group == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 更新群組的名稱、描述與基準幣
    /// </summary>
    /// <remarks>
    /// 基準幣僅在群組尚無任何花費與還款時可變更 —— 已鎖入的 Rate 是「原幣 → 基準幣」，
    /// 基準幣一變則全數失效，而重抓當前匯率會違反「Rate 代表消費發生當時的事實」；
    /// 還款金額本身就以基準幣記錄，同樣會跟著失去意義。
    /// </remarks>
    public async Task<Result<GroupResponse>> UpdateGroupAsync(int groupId, int userId, UpdateGroupRequest request)
    {
        var group = await FindAccessibleGroupAsync(groupId, userId);
        
        if (group == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        bool isCurrencyChanging = request.BaseCurrency.HasValue && request.BaseCurrency.Value != group.BaseCurrency;
        bool hasFinancialRecords = isCurrencyChanging && await dbContext.Groups
            .Where(storedGroup => storedGroup.Id == groupId)
            .AnyAsync(storedGroup => storedGroup.Expenses.Any() || storedGroup.Settlements.Any());

        if (hasFinancialRecords)
        {
            return Result<GroupResponse>.Failure(
                ResultCode.BusinessRuleViolation, "群組已有花費或還款紀錄，基準幣不可再變更");
        }

        // 動態敘述必須在改動之前組好
        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != group.Name)
        {
            changes.Add($"名稱由「{group.Name}」改為「{request.Name}」");
        }

        if (isCurrencyChanging)
        {
            changes.Add($"基準幣由 {group.BaseCurrency} 改為 {request.BaseCurrency!.Value}");
        }

        if (request.Description != null && request.Description != group.Description)
        {
            changes.Add("描述");
        }

        if (changes.Count == 0)
        {
            return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            group.Name = request.Name;
        }

        if (request.Description != null)
        {
            group.Description = request.Description;
        }

        if (request.BaseCurrency.HasValue)
        {
            group.BaseCurrency = request.BaseCurrency.Value;
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.GroupUpdated,
            Summary = $"修改了群組{string.Join("、", changes)}"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "更新群組失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 刪除群組（擁有者限定）
    /// </summary>
    /// <remarks>
    /// 不檢查未結清淨額或成員數 —— 「已結束」已吸收「想從首頁收起來但保留資料」的需求。
    /// 下游資料無須逐一軟刪，query filter 已串接群組的軟刪欄位。
    /// </remarks>
    public async Task<Result> DeleteGroupAsync(int groupId, int userId)
    {
        var group = await FindAccessibleGroupAsync(groupId, userId);

        if (group == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        if (group.OwnerUserId != userId)
        {
            return Result.Failure(ResultCode.Forbidden, "只有群組擁有者可以刪除群組");
        }

        dbContext.Groups.Remove(group);

        // 動態會隨群組一起被 query filter 濾掉，但資料留在 DB，事後仍查得到是誰刪的
        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.GroupDeleted,
            Summary = $"刪除了群組「{group.Name}」"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "刪除群組失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result.Success();
    }

    /// <summary>
    /// 設定群組的已結束狀態
    /// </summary>
    /// <remarks>
    /// 已結束只影響前端分區，不限制任何操作 —— 結束後仍可新增花費、記錄還款、查看結算。
    /// 由呼叫端帶目標狀態：任何成員都能操作。
    /// </remarks>
    public async Task<Result<GroupResponse>> SetGroupClosedAsync(int groupId, int userId, SetGroupClosedRequest request)
    {
        var group = await FindAccessibleGroupAsync(groupId, userId);

        if (group == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        bool shouldBeClosed = request.Closed!.Value;
        bool isClosed = group.ClosedAt != null;

        if (shouldBeClosed == isClosed)
        {
            return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
        }

        group.ClosedAt = shouldBeClosed ? DateTimeOffset.UtcNow : null;

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = shouldBeClosed ? ActivityActionType.GroupClosed : ActivityActionType.GroupReopened,
            Summary = shouldBeClosed
                ? $"將群組「{group.Name}」標記為已結束"
                : $"將群組「{group.Name}」改回進行中"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "設定群組結束狀態失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 重置邀請碼
    /// </summary>
    /// <remarks>
    /// 邀請碼本身即憑證，持有即可加入，因此重置是舊碼外流後唯一的撤銷手段。
    /// 任何成員皆可重置 —— 擁有者註銷帳號後仍須有人能撤銷外流的碼，是誰重置的由動態記錄。
    /// </remarks>
    public async Task<Result<GroupResponse>> ResetInviteCodeAsync(int groupId, int userId)
    {
        var group = await FindAccessibleGroupAsync(groupId, userId);

        if (group == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        group.InviteCode = InviteCodeGenerator.Generate();

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.InviteCodeReset,
            Summary = $"重置了群組「{group.Name}」的邀請碼"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "重置邀請碼失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    private async Task<Group?> FindAccessibleGroupAsync(int groupId, int userId)
        => await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(group => group.Id == groupId);
}
