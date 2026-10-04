using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Shared.Types;

namespace Project.Tests.Helpers;

/// <summary>
/// 觀察某位 User 能否存取種子群組
/// </summary>
/// <remarks>
/// 經由 GroupService 的公開介面讀取群組，而不是直接查 GroupMember 的綁定欄位。
/// </remarks>
public static class SplitBillAccess
{
    /// <summary>
    /// 以指定 User 的身分讀取種子群組，回傳結果碼
    /// </summary>
    public static async Task<ResultCode> ReadSeededGroupAsync(ApplicationDbContext context, int userId)
    {
        var groupService = new GroupService(context, NullLogger<GroupService>.Instance);

        return (await groupService.GetGroupByIdAsync(SplitBillSeeder.GroupId, userId)).Code;
    }
}
