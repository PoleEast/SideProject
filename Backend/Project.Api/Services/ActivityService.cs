using Microsoft.EntityFrameworkCore;
using Project.Api.Common;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;

namespace Project.Api.Services;

public class ActivityService(ApplicationDbContext dbContext)
{
    /// <summary>
    /// 取得群組動態，由新到舊
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    /// <param name="userId">呼叫者的使用者 ID</param>
    /// <param name="limit">只取最新的幾則；為 null 時全部回傳</param>
    /// <returns>
    /// 成功時為依記錄先後由新到舊的動態，每則帶著依成員 ID 排序的動態明細；
    /// 筆數不大於 0 回 ValidationError；群組不存在或呼叫者不是成員回 NotFound
    /// </returns>
    public async Task<Result<List<ActivityResponse>>> GetActivitiesAsync(int groupId, int userId, int? limit)
    {
        if (limit <= 0)
        {
            return Result<List<ActivityResponse>>.Failure(ResultCode.ValidationError, "筆數必須大於 0");
        }

        bool isAccessible = await dbContext.Groups.AccessibleBy(userId).AnyAsync(storedGroup => storedGroup.Id == groupId);

        if (!isAccessible)
        {
            return Result<List<ActivityResponse>>.Failure(ResultCode.NotFound, "找不到此群組");
        }

        // 只帶出動態明細，不經由成員的導覽屬性：已移除成員做過的動態與講到他的那一列都要留著
        IQueryable<ActivityLog> activityLogQuery = dbContext.ActivityLogs.AsNoTracking()
                                                                         .Include(activityLog => activityLog.Details)
                                                                         .Where(activityLog => activityLog.GroupId == groupId)
                                                                         .OrderByDescending(activityLog => activityLog.Id);

        if (limit.HasValue)
        {
            activityLogQuery = activityLogQuery.Take(limit.Value);
        }

        var activityLogs = await activityLogQuery.ToListAsync();

        return Result<List<ActivityResponse>>.Success(activityLogs.Select(activityLog => ToResponse(activityLog, userId)).ToList());
    }

    private static ActivityResponse ToResponse(ActivityLog activityLog, int userId) => new()
    {
        Id = activityLog.Id,
        ActionType = activityLog.ActionType,
        ActorName = activityLog.ActorName,
        IsMe = activityLog.ActorUserId == userId,
        Summary = activityLog.Summary,
        TargetExpenseId = activityLog.TargetExpenseId,
        CreatedAt = activityLog.CreatedAt,
        Details = activityLog.Details.OrderBy(detail => detail.GroupMemberId)
                                     .Select(detail => new ActivityDetailResponse { MemberId = detail.GroupMemberId, Text = detail.Text })
                                     .ToList()
    };
}
