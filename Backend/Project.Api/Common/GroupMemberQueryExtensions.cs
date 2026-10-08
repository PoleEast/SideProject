using Microsoft.EntityFrameworkCore;
using Project.Data.Model;

namespace Project.Api.Common;

public static class GroupMemberQueryExtensions
{
    /// <summary>
    /// 查出群組內指定成員的顯示名稱，包含已移除成員
    /// </summary>
    /// <remarks>
    /// 為了取得已移除成員而略過所有 query filter，群組本身是否存在須另行確認。
    /// </remarks>
    /// <param name="members">要查詢的成員</param>
    /// <param name="groupId">成員所屬群組的 ID</param>
    /// <param name="memberIds">要查的成員 ID</param>
    /// <returns>成員 ID 對應顯示名稱的字典；不屬於這個群組的 ID 不在其中</returns>
    public static Task<Dictionary<int, string>> FindDisplayNamesAsync(this IQueryable<GroupMember> members, int groupId, IEnumerable<int> memberIds)
        => members.IgnoreQueryFilters()
                  .Where(member => member.GroupId == groupId && memberIds.Contains(member.Id))
                  .ToDictionaryAsync(member => member.Id, member => member.DisplayName);

    /// <summary>
    /// 算出群組已用掉的名額，即含已移除成員的成員數
    /// </summary>
    /// <remarks>
    /// 為了計入已移除成員而略過所有 query filter，群組本身是否存在須另行確認。
    /// </remarks>
    /// <param name="members">要計數的成員</param>
    /// <param name="groupId">成員所屬群組的 ID</param>
    /// <returns>這個群組的成員數，現役成員與已移除成員都算</returns>
    public static Task<int> CountUsedSlotsAsync(this IQueryable<GroupMember> members, int groupId)
        => members.IgnoreQueryFilters().CountAsync(member => member.GroupId == groupId);
}
