using Microsoft.EntityFrameworkCore;
using Project.Api.Common;
using Project.Api.Helpers;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;

namespace Project.Api.Services;

public class GroupMemberService(ApplicationDbContext dbContext, SettlementService settlementService, ILogger<GroupMemberService> logger)
{
    /// <summary>
    /// 一次最多新增幾位成員
    /// </summary>
    /// <remarks>
    /// 見 SplitBill階段3B規格「批次新增」。
    /// </remarks>
    public const int MaxMembersPerBatch = 10;

    private const string ConcurrentChangeMessage = "成員資料剛被其他人修改，請重新整理後再試";

    /// <summary>
    /// 取得群組的所有成員，包含已移除者
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>成功時為依 ID 排序的成員；群組不存在或呼叫者不是成員回 NotFound</returns>
    public async Task<Result<List<GroupMemberResponse>>> GetMembersAsync(int groupId, int userId)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        // 群組本身已由存取檢查確認存在，這裡只需自行補上 GroupId 條件
        var members = await dbContext.GroupMembers
            .IgnoreQueryFilters() // 取得已移除者
            .Where(member => member.GroupId == groupId)
            .OrderBy(member => member.Id)
            .ToListAsync();

        var result = members.Select(member => ToResponse(member, userId, group.OwnerUserId)).ToList();

        return Result<List<GroupMemberResponse>>.Success(result);
    }

    /// <summary>
    /// 批次新增成員
    /// </summary>
    /// <remarks>
    /// 全部成功或全部失敗，一次請求只寫一筆ActivityLog。
    /// </remarks>
    /// <param name="groupId">群組 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <param name="request">要新增的顯示名稱</param>
    /// <returns>
    /// 成功時為新增的成員；人數不在 1~<see cref="MaxMembersPerBatch"/> 之間回 ValidationError；
    /// 群組不存在或呼叫者不是成員回 NotFound；
    /// 顯示名稱未通過 <see cref="MemberDisplayNameRules"/> 的驗證時回它的失敗結果；
    /// 儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result<List<GroupMemberResponse>>> AddMembersAsync(int groupId, int userId, AddGroupMembersRequest request)
    {
        if (request.DisplayNames.Count is 0 or > MaxMembersPerBatch)
        {
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.ValidationError, $"一次請新增 1~{MaxMembersPerBatch} 位成員");
        }

        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var activeNames = await dbContext.GroupMembers.Where(member => member.GroupId == groupId).Select(member => member.DisplayName).ToListAsync();

        var validation = MemberDisplayNameRules.Validate(request.DisplayNames, activeNames);

        if (!validation.IsSuccess)
        {
            return Result<List<GroupMemberResponse>>.Failure(validation);
        }

        var displayNames = validation.Value!;

        var members = displayNames
            .Select(displayName => new GroupMember { GroupId = groupId, DisplayName = displayName })
            .ToList();

        dbContext.AddRange(members);

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberAdded,
            Summary = $"新增了成員{StringFormatter.FormatNameList(displayNames)}"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "新增成員失敗。groupId: {GroupId}, userId: {UserId}", groupId, userId);
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        var result = members.Select(member => ToResponse(member, userId, group.OwnerUserId)).ToList();

        return Result<List<GroupMemberResponse>>.Success(result);
    }

    public async Task<Result<GroupMemberResponse>> RenameMemberAsync(
        int groupId, int memberId, int userId, RenameGroupMemberRequest request)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result<GroupMemberResponse>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var member = await dbContext.GroupMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.GroupId == groupId);

        if (member == null)
        {
            return Result<GroupMemberResponse>.Failure(ResultCode.NotFound, "找不到此成員");
        }

        // 排除本人，否則只改大小寫會撞到自己
        var otherNames = await dbContext.GroupMembers
            .Where(member => member.GroupId == groupId && member.Id != memberId)
            .Select(member => member.DisplayName)
            .ToListAsync();

        var validation = MemberDisplayNameRules.Validate(request.DisplayName, otherNames);

        if (!validation.IsSuccess)
        {
            return Result<GroupMemberResponse>.Failure(validation);
        }

        string displayName = validation.Value!;

        if (displayName == member.DisplayName)
        {
            return Result<GroupMemberResponse>.Success(ToResponse(member, userId, group.OwnerUserId));
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberRenamed,
            Summary = $"將成員「{member.DisplayName}」改名為「{displayName}」"
        });

        member.DisplayName = displayName;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<GroupMemberResponse>.Failure(ResultCode.Conflict, ConcurrentChangeMessage);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "成員改名失敗。groupId: {GroupId}, memberId: {MemberId}, userId: {UserId}",
                groupId, memberId, userId);
            return Result<GroupMemberResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        var result = ToResponse(member, userId, group.OwnerUserId);

        return Result<GroupMemberResponse>.Success(result);
    }

    public async Task<Result> RemoveMemberAsync(int groupId, int memberId, int userId)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var member = await dbContext.GroupMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.GroupId == groupId);

        if (member == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此成員");
        }

        if (member.UserId == group.OwnerUserId)
        {
            return Result.Failure(ResultCode.BusinessRuleViolation, "不可移除擁有者");
        }

        // 每一組兩兩淨額都要為 0，總和為 0 不算數
        var balances = await settlementService.GetBalancesAsync(group);
        var counterpartIds = balances
            .Where(balance => balance.DebtorMemberId == memberId || balance.CreditorMemberId == memberId)
            .Select(balance => balance.DebtorMemberId == memberId ? balance.CreditorMemberId : balance.DebtorMemberId)
            .ToList();

        if (counterpartIds.Count > 0)
        {
            var counterpartNames = await dbContext.GroupMembers
                .IgnoreQueryFilters()
                .Where(counterpart => counterpart.GroupId == groupId && counterpartIds.Contains(counterpart.Id))
                .OrderBy(counterpart => counterpart.Id)
                .Select(counterpart => counterpart.DisplayName)
                .ToListAsync();

            return Result.Failure(ResultCode.BusinessRuleViolation,
                $"「{member.DisplayName}」與{StringFormatter.FormatNameList(counterpartNames)}之間尚有未結清的淨額");
        }

        string summary = member.UserId == userId ? "退出了群組" : $"移除了成員「{member.DisplayName}」";

        // 有人因此失去存取權：不重置的話，他持舊碼就能立刻再加入
        if (member.UserId != null)
        {
            group.InviteCode = InviteCodeGenerator.Generate();
            summary += "，並重置了邀請碼";
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberRemoved,
            Summary = summary
        });

        dbContext.Remove(member);

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
            logger.LogError(ex, "移除成員失敗。groupId: {GroupId}, memberId: {MemberId}, userId: {UserId}",
                groupId, memberId, userId);
            return Result.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result.Success();
    }

    /// <summary>
    /// 解除成員的帳號綁定，位置與帳目保留
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    /// <param name="memberId">要解除綁定的成員 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>
    /// 解除完成、或成員原本就未綁定時為成功；群組不存在、呼叫者不是成員、或找不到成員回 NotFound；
    /// 對象是擁有者回 BusinessRuleViolation；成員同時被其他人修改回 Conflict；
    /// 儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result> UnbindMemberAsync(int groupId, int memberId, int userId)
    {
        var group = await dbContext.Groups.AccessibleBy(userId).FirstOrDefaultAsync(storedGroup => storedGroup.Id == groupId);

        if (group == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        var member = await dbContext.GroupMembers.FirstOrDefaultAsync(storedMember => storedMember.Id == memberId && storedMember.GroupId == groupId);

        if (member == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此成員");
        }

        if (member.UserId == group.OwnerUserId)
        {
            return Result.Failure(ResultCode.BusinessRuleViolation, "不可解除擁有者的綁定");
        }
        if (member.UserId == null)
        {
            return Result.Success();
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberUnbound,

            // 解除自己要寫出位置名稱：位置與帳目還在，其他人需要知道它現在是空位
            Summary = member.UserId == userId
                ? $"解除了與「{member.DisplayName}」的綁定並退出群組，並重置了邀請碼"
                : $"解除了「{member.DisplayName}」的帳號綁定，並重置了邀請碼"
        });

        member.UserId = null;
        group.InviteCode = InviteCodeGenerator.Generate();

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
            logger.LogError(ex, "解除綁定失敗。groupId: {GroupId}, memberId: {MemberId}, userId: {UserId}",
                groupId, memberId, userId);
            return Result.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result.Success();
    }

    private static GroupMemberResponse ToResponse(GroupMember member, int userId, int ownerUserId) => new()
    {
        Id = member.Id,
        DisplayName = member.DisplayName,
        IsRemoved = member.DeletedAt != null,
        IsMe = member.UserId == userId,
        IsOwner = member.UserId == ownerUserId,
        IsBound = member.UserId != null
    };
}
