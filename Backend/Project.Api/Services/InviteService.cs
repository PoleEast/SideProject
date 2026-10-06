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

/// <remarks>
/// 呼叫者此時還不是成員，入口是邀請碼而非 groupId
/// </remarks>
public class InviteService(ApplicationDbContext dbContext, ILogger<InviteService> logger)
{
    public async Task<Result<InvitePreviewResponse>> PreviewAsync(string inviteCode, int userId)
    {
        var group = await FindGroupAsync(inviteCode);

        if (group == null)
        {
            return Result<InvitePreviewResponse>.Failure(ResultCode.NotFound, "邀請碼無效或已被重置");
        }

        string? userName = await dbContext.Users.AsNoTracking().Where(user => user.Id == userId).Select(user => user.Name).FirstOrDefaultAsync();

        if (userName == null)
        {
            return Result<InvitePreviewResponse>.Failure(ResultCode.Unauthorized, "使用者不存在");
        }

        var members = await dbContext.GroupMembers.Where(member => member.GroupId == group.Id).OrderBy(member => member.Id).ToListAsync();

        var result = new InvitePreviewResponse
        {
            GroupId = group.Id,
            GroupName = group.Name,
            IsMember = members.Any(member => member.UserId == userId),
            SuggestedDisplayName = userName,
            Members = members.Select(member => new InvitePreviewMemberResponse
            {
                Id = member.Id,
                DisplayName = member.DisplayName,
                IsBound = member.UserId != null
            }).ToList()
        };

        return Result<InvitePreviewResponse>.Success(result);
    }

    /// <summary>
    /// 認領一位未綁定的既有成員
    /// </summary>
    /// <param name="inviteCode">邀請碼</param>
    /// <param name="memberId">要認領的成員 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>
    /// 成功時為加入的群組；邀請碼無效或找不到成員回 NotFound；
    /// 呼叫者已是成員、或該成員已被認領回 Conflict；儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result<GroupResponse>> ClaimMemberAsync(string inviteCode, int memberId, int userId)
    {
        var joinableGroup = await FindJoinableGroupAsync(inviteCode, userId);

        if (!joinableGroup.IsSuccess || joinableGroup.Value is null)
        {
            return Result<GroupResponse>.Failure(joinableGroup);
        }

        var group = joinableGroup.Value;

        var member = await dbContext.GroupMembers.FirstOrDefaultAsync(storedMember => storedMember.Id == memberId && storedMember.GroupId == group.Id);

        if (member == null)
        {
            return Result<GroupResponse>.Failure(ResultCode.NotFound, "找不到此成員");
        }

        if (member.UserId != null)
        {
            return Result<GroupResponse>.Failure(ResultCode.Conflict, "這個位置已被認領");
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = group.Id,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberClaimed,
            Summary = $"認領了成員「{member.DisplayName}」"
        });

        member.UserId = userId;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<GroupResponse>.Failure(ResultCode.Conflict, "這個位置剛被認領");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "認領成員失敗。groupId: {GroupId}, memberId: {MemberId}, userId: {UserId}",
                group.Id, memberId, userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 建立一位綁定呼叫者的新成員
    /// </summary>
    /// <param name="inviteCode">邀請碼</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <param name="request">新成員的顯示名稱</param>
    /// <returns>
    /// 成功時為加入的群組；邀請碼無效回 NotFound；呼叫者已是成員回 Conflict；
    /// 顯示名稱未通過 <see cref="MemberDisplayNameRules"/> 的驗證時回它的失敗結果；
    /// 儲存失敗回 InternalServerError
    /// </returns>
    public async Task<Result<GroupResponse>> JoinAsNewMemberAsync(string inviteCode, int userId, JoinAsNewMemberRequest request)
    {
        var joinableGroup = await FindJoinableGroupAsync(inviteCode, userId);

        if (!joinableGroup.IsSuccess)
        {
            return Result<GroupResponse>.Failure(joinableGroup);
        }

        var group = joinableGroup.Value!;

        var activeNames = await dbContext.GroupMembers.Where(member => member.GroupId == group.Id).Select(member => member.DisplayName).ToListAsync();

        var validation = MemberDisplayNameRules.Validate(request.DisplayName, activeNames);

        if (!validation.IsSuccess)
        {
            return Result<GroupResponse>.Failure(validation);
        }

        string displayName = validation.Value!;

        dbContext.Add(new GroupMember { 
            GroupId = group.Id, 
            DisplayName = displayName, 
            UserId = userId
        });

        dbContext.Add(new ActivityLog
        {
            GroupId = group.Id,
            ActorUserId = userId,
            ActionType = ActivityActionType.MemberJoined,
            Summary = $"以「{displayName}」加入了群組"
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "以新成員加入失敗。groupId: {GroupId}, userId: {UserId}", group.Id, userId);
            return Result<GroupResponse>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
        }

        return Result<GroupResponse>.Success(group.Adapt<GroupResponse>());
    }

    /// <summary>
    /// 找出邀請碼對應的群組，並確認呼叫者還不是它的成員
    /// </summary>
    /// <remarks>
    /// 「已是成員」與 AccessibleBy 同一個判準。
    /// </remarks>
    /// <param name="inviteCode">邀請碼</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <returns>成功時為該群組；邀請碼無效回 NotFound；呼叫者已是成員回 Conflict</returns>
    private async Task<Result<Group>> FindJoinableGroupAsync(string inviteCode, int userId)
    {
        var group = await FindGroupAsync(inviteCode);

        if (group == null)
        {
            return Result<Group>.Failure(ResultCode.NotFound, "邀請碼無效或已被重置");
        }

        if (await dbContext.Groups.AccessibleBy(userId).AnyAsync(storedGroup => storedGroup.Id == group.Id))
        {
            return Result<Group>.Failure(ResultCode.Conflict, "你已經是這個群組的成員");
        }

        return Result<Group>.Success(group);
    }

    /// <summary>
    /// 以邀請碼找出群組
    /// </summary>
    /// <remarks>
    /// 先正規化再比對，不吃資料庫大小寫定序設定
    /// </remarks>
    /// <param name="inviteCode">使用者輸入的邀請碼</param>
    /// <returns>對應的群組；找不到時為 null</returns>
    private Task<Group?> FindGroupAsync(string inviteCode)
    {
        string normalizedCode = InviteCodeGenerator.Normalize(inviteCode);

        return dbContext.Groups.FirstOrDefaultAsync(storedGroup => storedGroup.InviteCode == normalizedCode);
    }
}
