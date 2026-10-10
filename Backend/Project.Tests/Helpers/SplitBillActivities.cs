using Project.Api.Services;
using Project.Data;
using Project.Shared.DTOs.SplitBill;

namespace Project.Tests.Helpers;

/// <summary>
/// 觀察群組動態
/// </summary>
/// <remarks>
/// 經由讀取動態的公開介面觀察，而不是直接查 ActivityLog 資料表。
/// </remarks>
public static class SplitBillActivities
{
    /// <summary>
    /// 以指定 User 的身分讀取種子群組的動態，由新到舊
    /// </summary>
    public static Task<List<ActivityResponse>> ReadSeededGroupAsync(ApplicationDbContext context, int userId, int? limit = null)
        => ReadAsync(context, SplitBillSeeder.GroupId, userId, limit);

    /// <summary>
    /// 以指定 User 的身分讀取某個群組的動態，由新到舊
    /// </summary>
    public static async Task<List<ActivityResponse>> ReadAsync(ApplicationDbContext context, int groupId, int userId, int? limit = null)
    {
        var service = new ActivityService(context);

        return (await service.GetActivitiesAsync(groupId, userId, limit)).Value!;
    }
}
