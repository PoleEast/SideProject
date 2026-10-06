using Project.Data.Model;

namespace Project.Api.Common;

public static class GroupQueryExtensions
{
    /// <summary>
    /// 篩選出指定使用者有權存取的群組
    /// </summary>
    /// <remarks>
    /// 擁有者在建立群組時即成為成員，因此只需判斷有無 GroupMember，不需要額外的擁有者分支。
    /// 不必自行加上 <c>DeletedAt == null</c> —— GroupMember 的 query filter 會自動套用到這個導覽屬性上，被移除的成員本來就不算數。
    /// </remarks>
    /// <param name="groups">要篩選的群組查詢</param>
    /// <param name="userId">使用者 ID</param>
    /// <returns>只留下該使用者是現役成員的群組</returns>
    public static IQueryable<Group> AccessibleBy(this IQueryable<Group> groups, int userId)
        => groups.Where(group => group.GroupMembers.Any(member => member.UserId == userId));
}
