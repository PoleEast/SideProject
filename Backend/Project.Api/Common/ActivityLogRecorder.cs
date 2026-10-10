using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.Types;

namespace Project.Api.Common;

/// <summary>
/// 群組動態的記錄入口
/// </summary>
/// <remarks>
/// 操作者的名稱與當時的成員一律由這裡填入。動態只加入、不存檔，須與它記錄的變動在同一次存檔寫入。
/// </remarks>
public static class ActivityLogRecorder
{
    /// <summary>
    /// 記下一則由現役成員做出的動態
    /// </summary>
    /// <remarks>
    /// 操作者的名稱取資料庫中已存檔的顯示名稱，也就是這次變動發生前的名字。
    /// </remarks>
    /// <param name="dbContext">動態要加入的 DbContext</param>
    /// <param name="groupId">動態所屬群組的 ID</param>
    /// <param name="userId">操作者的使用者 ID</param>
    /// <param name="actionType">變動類型</param>
    /// <param name="summary">敘述，不含主詞</param>
    /// <param name="targetExpense">這則動態講的花費；與花費無關時為 null</param>
    /// <param name="details">動態明細；沒有時為 null</param>
    /// <returns>加入完成時為成功；操作者在這個群組已沒有綁定的現役成員回 NotFound</returns>
    public static async Task<Result> RecordActivityAsync(
        this ApplicationDbContext dbContext, int groupId, int userId, ActivityActionType actionType, string summary,
        Expense? targetExpense = null, IEnumerable<ActivityLogDetail>? details = null)
    {
        var actor = await dbContext.GroupMembers.Where(member => member.GroupId == groupId && member.UserId == userId)
                                                .Select(member => new { member.Id, member.DisplayName })
                                                .FirstOrDefaultAsync();

        // 存取檢查之後才被移除或解除綁定，視同已失去存取權
        if (actor == null)
        {
            return Result.Failure(ResultCode.NotFound, "找不到此群組");
        }

        dbContext.Add(new ActivityLog
        {
            GroupId = groupId,
            ActorUserId = userId,
            ActorMemberId = actor.Id,
            ActorName = actor.DisplayName,
            ActionType = actionType,
            TargetExpense = targetExpense,
            Summary = summary,
            Details = details?.ToList() ?? []
        });

        return Result.Success();
    }

    /// <summary>
    /// 記下一則操作者這次才成為成員的動態
    /// </summary>
    /// <remarks>
    /// 變動發生前他還不是成員，名稱因此取他註冊時填的名字。
    /// </remarks>
    /// <param name="dbContext">動態要加入的 DbContext</param>
    /// <param name="joinedMember">操作者這次取得的成員；所屬群組尚未存檔時，須已設在它的導覽屬性上</param>
    /// <param name="userId">操作者的使用者 ID</param>
    /// <param name="actionType">變動類型</param>
    /// <param name="summary">敘述，不含主詞</param>
    /// <returns>加入完成時為成功；使用者不存在回 Unauthorized</returns>
    public static async Task<Result> RecordJoiningActivityAsync(
        this ApplicationDbContext dbContext, GroupMember joinedMember, int userId, ActivityActionType actionType, string summary)
    {
        string? registeredName = await dbContext.Users.Where(user => user.Id == userId)
                                                      .Select(user => user.Name)
                                                      .FirstOrDefaultAsync();

        if (registeredName == null)
        {
            return Result.Failure(ResultCode.Unauthorized, "使用者不存在");
        }

        dbContext.Add(new ActivityLog
        {
            // 群組與成員同一次存檔才建立時還沒有 Id，由導覽屬性連結；已存檔的則已有外鍵值
            Group = joinedMember.Group,
            GroupId = joinedMember.GroupId,
            ActorUserId = userId,
            ActorMember = joinedMember,
            ActorName = registeredName,
            ActionType = actionType,
            Summary = summary
        });

        return Result.Success();
    }
}
