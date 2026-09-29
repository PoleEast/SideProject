using Microsoft.EntityFrameworkCore;
using Project.Api.Common;
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
    private const int MaxMembersPerBatch = 10;

    /// <summary>
    /// 顯示名稱的長度上限，與資料庫欄位一致
    /// </summary>
    private const int MaxDisplayNameLength = 32;

    private static readonly string InvalidDisplayNameMessage = $"顯示名稱長度請為 1~{MaxDisplayNameLength} 個字元";

    /// <summary>
    /// 取得群組的所有成員，包含已移除者
    /// </summary>
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

        // 正規化
        var displayNames = request.DisplayNames.Select(displayName => displayName?.Trim() ?? string.Empty).ToList();

        if (displayNames.Any(displayName => displayName.Length is 0 or > MaxDisplayNameLength))
        {
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.ValidationError, InvalidDisplayNameMessage);
        }

        // 撈進記憶體再不分大小寫比對，SQL Server 預設定序不分大小寫
        var activeNames = await dbContext.GroupMembers
            .Where(member => member.GroupId == groupId)
            .Select(member => member.DisplayName)
            .ToListAsync();

        // 先放進未被移除的成員名字，Add 失敗等於撞名
        var takenNames = new HashSet<string>(activeNames, StringComparer.OrdinalIgnoreCase);
        var duplicateNames = displayNames
            .Where(displayName => !takenNames.Add(displayName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            return Result<List<GroupMemberResponse>>.Failure(ResultCode.Conflict, $"以下名稱與其他成員重複：{QuoteNames(duplicateNames)}");
        }

        var members = displayNames
            .Select(displayName => new GroupMember { GroupId = groupId, DisplayName = displayName })
            .ToList();

        dbContext.AddRange(members);

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberAdded,
            Summary = $"新增了成員{QuoteNames(displayNames)}"
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

    /// <summary>
    /// 修改成員的顯示名稱
    /// </summary>
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

        string displayName = request.DisplayName.Trim();

        if (displayName.Length is 0 or > MaxDisplayNameLength)
        {
            return Result<GroupMemberResponse>.Failure(ResultCode.ValidationError, InvalidDisplayNameMessage);
        }

        if (displayName == member.DisplayName)
        {
            return Result<GroupMemberResponse>.Success(ToResponse(member, userId, group.OwnerUserId));
        }

        // 排除本人，否則只改大小寫會撞到自己
        var otherNames = await dbContext.GroupMembers
            .Where(member => member.GroupId == groupId && member.Id != memberId)
            .Select(member => member.DisplayName)
            .ToListAsync();

        if (otherNames.Contains(displayName, StringComparer.OrdinalIgnoreCase))
        {
            return Result<GroupMemberResponse>.Failure(
                ResultCode.Conflict, $"以下名稱與其他成員重複：{QuoteNames([displayName])}");
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
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "成員改名失敗。groupId: {GroupId}, memberId: {MemberId}, userId: {UserId}",
                groupId, memberId, userId);
            return Result<GroupMemberResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        var result = ToResponse(member, userId, group.OwnerUserId);

        return Result<GroupMemberResponse>.Success(result);
    }

    /// <summary>
    /// 移除成員
    /// </summary>
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
                $"「{member.DisplayName}」與{QuoteNames(counterpartNames)}之間尚有未結清的淨額");
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberRemoved,
            Summary = member.UserId == userId ? "退出了群組" : $"移除了成員「{member.DisplayName}」"
        });

        dbContext.Remove(member);

        try
        {
            await dbContext.SaveChangesAsync();
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
    /// 組成「「小美」、「阿德」」這種動態與錯誤訊息用的名稱清單
    /// </summary>
    private static string QuoteNames(IEnumerable<string> names) => string.Join("、", names.Select(name => $"「{name}」"));

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
