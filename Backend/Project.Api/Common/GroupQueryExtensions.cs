using Project.Data.Model;

namespace Project.Api.Common;

/// <summary>
/// 群組查詢的共用條件
/// </summary>
public static class GroupQueryExtensions
{
    /// <summary>
    /// 篩選出指定使用者有權存取的群組
    /// </summary>
    /// <remarks>
    /// 擁有者在建立群組時即成為成員，因此只需判斷有無 GroupMember，不需要額外的擁有者分支。
    /// 不必自行加上 <c>DeletedAt == null</c> —— GroupMember 的 query filter 會自動套用到這個導覽屬性上，被移除的成員本來就不算數。
    /// </remarks>
    public static IQueryable<Group> AccessibleBy(this IQueryable<Group> groups, int userId)
        => groups.Where(group => group.GroupMembers.Any(member => member.UserId == userId));
}
