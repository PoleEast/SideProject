using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Helpers;
using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// GroupService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證存取檢查、擁有者特權、基準幣凍結與動態寫入
/// </summary>
/// <remarks>
/// 邀請碼的唯一索引不在此驗證：InMemory provider 不強制唯一約束，寫出來的測試會恆過。
/// 該項改由人工檢視 migration 產出的 SQL 把關。
/// </remarks>
public class GroupServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int MingUserId = 1;
    private const int AmyUserId = 2;

    private static GroupService CreateService(ApplicationDbContext context)
        => new(context, NullLogger<GroupService>.Instance);

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        context.Users.AddRange(
            new User { Id = MingUserId, Account = "ming", PasswordHash = "not_used", Name = "小明" },
            new User { Id = AmyUserId, Account = "amy", PasswordHash = "not_used", Name = "Amy" });

        await context.SaveChangesAsync(Ct);
    }

    private static CreateGroupRequest NewGroupRequest(string name = "日本旅遊") => new()
    {
        Name = name,
        Description = "2026 夏天",
        BaseCurrency = CurrencyType.TWD
    };

    private static async Task<GroupMember> AddAmyAsMemberAsync(ApplicationDbContext context, int groupId)
    {
        var amy = new GroupMember { GroupId = groupId, UserId = AmyUserId, DisplayName = "Amy" };
        context.GroupMembers.Add(amy);
        await context.SaveChangesAsync(Ct);

        return amy;
    }

    /// <summary>
    /// 新增一筆由小明墊付的日圓花費
    /// </summary>
    private static async Task AddExpenseAsync(ApplicationDbContext context, int groupId)
    {
        var payer = await context.GroupMembers
            .SingleAsync(member => member.GroupId == groupId && member.UserId == MingUserId, Ct);

        context.Expenses.Add(new Expense
        {
            GroupId = groupId,
            PayerId = payer.Id,
            Name = "晚餐",
            Category = ExpenseCategoryType.Food,
            Currency = CurrencyType.JPY,
            Amount = 9000m,
            Rate = 0.212345m,
            Date = new DateTime(2026, 7, 1),
            CreatedByUserId = MingUserId
        });
        await context.SaveChangesAsync(Ct);
    }

    #region 建立群組

    [Fact(DisplayName = "建立群組：擁有者自動成為成員，顯示名稱取自 User.Name")]
    public async Task CreateGroup_OwnerBecomesMemberWithUserName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Assert
        Assert.True(result.IsSuccess);

        var member = await context.GroupMembers.SingleAsync(Ct);
        Assert.Equal(MingUserId, member.UserId);
        Assert.Equal("小明", member.DisplayName);
        Assert.Equal(result.Value!.Id, member.GroupId);
    }

    [Fact(DisplayName = "建立群組：邀請碼為 8 碼，且全部取自指定字元集")]
    public async Task CreateGroup_InviteCodeMatchesFormat()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Assert
        string inviteCode = result.Value!.InviteCode;
        Assert.Equal(InviteCodeGenerator.CodeLength, inviteCode.Length);
        Assert.All(inviteCode, character => Assert.Contains(character, InviteCodeGenerator.Alphabet));
    }

    [Fact(DisplayName = "建立群組：兩次建立不會拿到同一組邀請碼")]
    public async Task CreateGroup_InviteCodesDiffer()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        var first = await service.CreateGroupAsync(MingUserId, NewGroupRequest("日本旅遊"));
        var second = await service.CreateGroupAsync(MingUserId, NewGroupRequest("週五晚餐"));

        // Assert
        Assert.NotEqual(first.Value!.InviteCode, second.Value!.InviteCode);
    }

    [Fact(DisplayName = "建立群組：寫入一筆建立群組的動態")]
    public async Task CreateGroup_WritesActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Assert
        var log = await context.ActivityLogs.SingleAsync(Ct);
        Assert.Equal(ActivityActionType.GroupCreated, log.ActionType);
        Assert.Equal(MingUserId, log.ActorUserId);
        Assert.Contains("日本旅遊", log.Summary);
    }

    [Fact(DisplayName = "建立群組：使用者不存在時回 Unauthorized")]
    public async Task CreateGroup_UnknownUser_ReturnsUnauthorized()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(999, NewGroupRequest());

        // Assert
        Assert.Equal(ResultCode.Unauthorized, result.Code);
        Assert.Empty(await context.Groups.ToListAsync(Ct));
    }

    #endregion

    #region 存取檢查

    [Fact(DisplayName = "查詢群組清單：只回傳自己參與的群組")]
    public async Task GetMyGroups_ReturnsOnlyOwnGroups()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        await service.CreateGroupAsync(MingUserId, NewGroupRequest("小明的群組"));
        await service.CreateGroupAsync(AmyUserId, NewGroupRequest("Amy 的群組"));

        // Act
        var result = await service.GetMyGroupsAsync(MingUserId);

        // Assert
        var group = Assert.Single(result.Value!);
        Assert.Equal("小明的群組", group.Name);
    }

    [Fact(DisplayName = "查詢單一群組：非成員回 NotFound 而非 Forbidden")]
    public async Task GetGroupById_NotAMember_ReturnsNotFound()
    {
        // Arrange - 群組存在，但 Amy 不是成員
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.GetGroupByIdAsync(created.Value!.Id, AmyUserId);

        // Assert - 不區分 404／403，否則能用遞增 id 掃出哪些群組存在
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    [Fact(DisplayName = "更新群組：非成員回 NotFound")]
    public async Task UpdateGroup_NotAMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, AmyUserId, new UpdateGroupRequest { Name = "被別人改名" });

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Equal("日本旅遊", (await context.Groups.SingleAsync(Ct)).Name);
    }

    #endregion

    #region 更新與基準幣凍結

    [Fact(DisplayName = "更新群組：尚無花費與還款時可變更基準幣")]
    public async Task UpdateGroup_NoFinancialRecords_CanChangeBaseCurrency()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { BaseCurrency = CurrencyType.JPY });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CurrencyType.JPY, result.Value!.BaseCurrency);
    }

    [Fact(DisplayName = "更新群組：已有花費時變更基準幣被擋下")]
    public async Task UpdateGroup_HasExpenses_CannotChangeBaseCurrency()
    {
        // Arrange - 已鎖入的 Rate 是「原幣 → 基準幣」，基準幣一變則全數失效
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        await AddExpenseAsync(context, created.Value!.Id);

        // 模擬新的 request：否則 EF 的 relationship fix-up 會把剛加入的花費塞進仍被追蹤的 group.Expenses，
        // 讓「直接讀未 Include 的導覽屬性」這種錯誤寫法在測試裡也能過
        context.ChangeTracker.Clear();

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value.Id, MingUserId, new UpdateGroupRequest { BaseCurrency = CurrencyType.JPY });

        // Assert
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Equal(CurrencyType.TWD, (await context.Groups.SingleAsync(Ct)).BaseCurrency);
    }

    [Fact(DisplayName = "更新群組：沒有花費但已有還款時，變更基準幣仍被擋下")]
    public async Task UpdateGroup_HasSettlementsOnly_CannotChangeBaseCurrency()
    {
        // Arrange - 還款金額以基準幣記錄，基準幣一變就失去意義，即使群組裡沒有任何花費
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        var ming = await context.GroupMembers.SingleAsync(Ct);
        var amy = await AddAmyAsMemberAsync(context, created.Value!.Id);

        context.Settlements.Add(new Settlement
        {
            GroupId = created.Value.Id,
            FromMemberId = amy.Id,
            ToMemberId = ming.Id,
            Amount = 500m,
            SettledAt = new DateTime(2026, 7, 5),
            CreatedByUserId = MingUserId
        });
        await context.SaveChangesAsync(Ct);

        // 模擬新的 request，理由同上
        context.ChangeTracker.Clear();

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value.Id, MingUserId, new UpdateGroupRequest { BaseCurrency = CurrencyType.JPY });

        // Assert
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Equal(CurrencyType.TWD, (await context.Groups.SingleAsync(Ct)).BaseCurrency);
    }

    [Fact(DisplayName = "更新群組：已有花費時仍可改名")]
    public async Task UpdateGroup_HasExpenses_CanStillRename()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        await AddExpenseAsync(context, created.Value!.Id);

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value.Id, MingUserId, new UpdateGroupRequest { Name = "日本旅遊 2026" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("日本旅遊 2026", result.Value!.Name);
    }

    [Fact(DisplayName = "更新群組：動態敘述含變動前後值")]
    public async Task UpdateGroup_ActivityLogContainsBeforeAndAfter()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Name = "日本旅遊 2026" });

        // Assert - 舊值必須在改動前就取出，否則會寫成「由新值改為新值」
        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct);
        // 斷言整句而非分別 Contains：新名稱本身就包含舊名稱，分開斷言在「由新值改為新值」時照樣會過
        Assert.Equal("修改了群組名稱由「日本旅遊」改為「日本旅遊 2026」", log.Summary);
    }

    [Fact(DisplayName = "更新群組：送出的值與現況相同時不寫動態")]
    public async Task UpdateGroup_NoActualChange_WritesNoActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Name = "日本旅遊" });

        // Assert - 只剩建立群組那一筆
        Assert.True(result.IsSuccess);
        var log = await context.ActivityLogs.SingleAsync(Ct);
        Assert.Equal(ActivityActionType.GroupCreated, log.ActionType);
    }

    [Fact(DisplayName = "更新群組：描述送空字串會清空描述並寫動態")]
    public async Task UpdateGroup_EmptyDescription_ClearsDescription()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Description = "" });

        // Assert - 空字串代表清空，不能跟 null 一樣被當成「未傳入」而略過
        Assert.True(result.IsSuccess);
        Assert.Equal("", result.Value!.Description);

        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct);
        Assert.Equal("修改了群組描述", log.Summary);
    }

    #endregion

    #region 擁有者特權

    [Fact(DisplayName = "刪除群組：非擁有者的成員回 Forbidden")]
    public async Task DeleteGroup_NotOwner_ReturnsForbidden()
    {
        // Arrange - Amy 是成員但不是擁有者
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        await AddAmyAsMemberAsync(context, created.Value!.Id);

        // Act
        var result = await service.DeleteGroupAsync(created.Value.Id, AmyUserId);

        // Assert - 此時已確定是成員，群組存在與否不再是秘密，因此回 403 而非 404
        Assert.Equal(ResultCode.Forbidden, result.Code);
        Assert.Single(await context.Groups.ToListAsync(Ct));
    }

    [Fact(DisplayName = "刪除群組：擁有者可刪，下游資料一併查不到")]
    public async Task DeleteGroup_AsOwner_CascadesToDownstream()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // 模擬新的一次請求：實際執行時每個請求各有獨立的 DbContext，刪除只會載入群組本身。
        // 不清空的話成員與建立動態仍被追蹤，Remove 會判定它們的必要關聯遭切斷而拋錯。
        context.ChangeTracker.Clear();

        // Act
        var result = await service.DeleteGroupAsync(created.Value!.Id, MingUserId);

        // Assert - 無須逐一軟刪，query filter 已串接群組的軟刪欄位
        Assert.True(result.IsSuccess);
        Assert.Empty(await context.Groups.ToListAsync(Ct));
        Assert.Empty(await context.GroupMembers.ToListAsync(Ct));
        Assert.Empty(await context.ActivityLogs.ToListAsync(Ct));
    }

    #endregion

    #region 邀請碼

    [Fact(DisplayName = "重置邀請碼：非擁有者的成員也能重置，動態記下實際操作者")]
    public async Task ResetInviteCode_NonOwnerMember_SucceedsAndLogsActor()
    {
        // Arrange - Amy 是成員但不是擁有者
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        string originalCode = created.Value!.InviteCode;
        await AddAmyAsMemberAsync(context, created.Value.Id);

        // Act
        var result = await service.ResetInviteCodeAsync(created.Value.Id, AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(originalCode, result.Value!.InviteCode);

        // 不限制誰能撤銷，歸屬改由動態提供
        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.InviteCodeReset, Ct);
        Assert.Equal(AmyUserId, log.ActorUserId);
    }

    [Fact(DisplayName = "重置邀請碼：非成員回 NotFound")]
    public async Task ResetInviteCode_NotAMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.ResetInviteCodeAsync(created.Value!.Id, AmyUserId);

        // Assert - 重置不設擁有者限制，存取檢查是唯一防線
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    [Fact(DisplayName = "重置邀請碼：碼會改變，且動態不外洩邀請碼")]
    public async Task ResetInviteCode_ChangesCodeAndKeepsItOutOfTheLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        string originalCode = created.Value!.InviteCode;

        // Act
        var result = await service.ResetInviteCodeAsync(created.Value.Id, MingUserId);

        // Assert
        Assert.NotEqual(originalCode, result.Value!.InviteCode);

        // 動態牆是群組內公開的，寫進邀請碼等於把憑證貼在牆上
        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.InviteCodeReset, Ct);
        Assert.DoesNotContain(originalCode, log.Summary);
        Assert.DoesNotContain(result.Value.InviteCode, log.Summary);
    }

    #endregion

    #region 已結束狀態

    [Fact(DisplayName = "設定已結束：送 true 設定時間，送 false 清空")]
    public async Task SetClosed_ClosesAndReopens()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var closed = await service.SetGroupClosedAsync(
            created.Value!.Id, MingUserId, new SetGroupClosedRequest { Closed = true });
        var reopened = await service.SetGroupClosedAsync(
            created.Value.Id, MingUserId, new SetGroupClosedRequest { Closed = false });

        // Assert
        Assert.NotNull(closed.Value!.ClosedAt);
        Assert.Null(reopened.Value!.ClosedAt);
    }

    [Fact(DisplayName = "設定已結束：已是結束狀態再送 true，不重開、不改結束時間、不多寫動態")]
    public async Task SetClosed_AlreadyClosed_IsNoOp()
    {
        // Arrange - Amy 先結束了群組，Ming 的畫面還停在「進行中」
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());
        await AddAmyAsMemberAsync(context, created.Value!.Id);

        var closedByAmy = await service.SetGroupClosedAsync(
            created.Value.Id, AmyUserId, new SetGroupClosedRequest { Closed = true });

        // Act - Ming 依過期的畫面也按了結束；連點或逾時重按也是同一個情境
        var result = await service.SetGroupClosedAsync(
            created.Value.Id, MingUserId, new SetGroupClosedRequest { Closed = true });

        // Assert - 若是切換，群組會被重開，還會以 Ming 的名義記下 GroupReopened
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.ClosedAt);
        Assert.Equal(closedByAmy.Value!.ClosedAt, result.Value.ClosedAt);

        var closedStateLogs = await context.ActivityLogs
            .Where(activityLog => activityLog.ActionType == ActivityActionType.GroupClosed
                || activityLog.ActionType == ActivityActionType.GroupReopened)
            .ToListAsync(Ct);
        var log = Assert.Single(closedStateLogs);
        Assert.Equal(ActivityActionType.GroupClosed, log.ActionType);
        Assert.Equal(AmyUserId, log.ActorUserId);
    }

    [Fact(DisplayName = "設定已結束：結束與重啟寫入不同的動態類型")]
    public async Task SetClosed_WritesDistinctActivityTypes()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        await service.SetGroupClosedAsync(
            created.Value!.Id, MingUserId, new SetGroupClosedRequest { Closed = true });
        await service.SetGroupClosedAsync(
            created.Value.Id, MingUserId, new SetGroupClosedRequest { Closed = false });

        // Assert - 前端靠 ActionType 決定圖示與顏色，兩者不可混為一談
        var actionTypes = await context.ActivityLogs
            .Select(activityLog => activityLog.ActionType)
            .ToListAsync(Ct);
        Assert.Contains(ActivityActionType.GroupClosed, actionTypes);
        Assert.Contains(ActivityActionType.GroupReopened, actionTypes);
    }

    [Fact(DisplayName = "設定已結束：非成員回 NotFound")]
    public async Task SetClosed_NotAMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.SetGroupClosedAsync(
            created.Value!.Id, AmyUserId, new SetGroupClosedRequest { Closed = true });

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    #endregion
}
