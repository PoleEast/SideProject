using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Helpers;
using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.Constants;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Project.Tests;

/// <summary>
/// GroupService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證存取檢查、擁有者特權、基準幣凍結、動態寫入，以及群組清單的個人淨額與結清狀態
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
        => new(context, new SettlementService(context, NullLogger<SettlementService>.Instance), NullLogger<GroupService>.Instance);

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        context.Users.AddRange(
            new User { Id = MingUserId, Account = "ming", PasswordHash = "not_used", Name = "小明" },
            new User { Id = AmyUserId, Account = "amy", PasswordHash = "not_used", Name = "Amy" });

        await context.SaveChangesAsync(Ct);
    }

    private static CreateGroupRequest NewGroupRequest(string name = "日本旅遊", GroupIconType icon = GroupIconType.Travel) => new()
    {
        Name = name,
        Description = "2026 夏天",
        BaseCurrency = CurrencyType.TWD,
        Icon = icon
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
            Date = new DateOnly(2026, 7, 1),
            CreatedByUserId = MingUserId
        });
        await context.SaveChangesAsync(Ct);
    }

    private static async Task<GroupListItemResponse> ReadListItemAsync(ApplicationDbContext context, int userId, int groupId)
        => (await CreateService(context).GetMyGroupsAsync(userId)).Value!.Single(item => item.Id == groupId);

    /// <summary>
    /// 讓種子群組裡的 Amy 綁定 AmyUserId，從她的角度讀得到群組清單
    /// </summary>
    private static async Task BindSeededAmyAsync(ApplicationDbContext context)
    {
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "Amy");
        var amy = await context.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        amy.UserId = AmyUserId;
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 在種子群組補種一筆還款
    /// </summary>
    private static async Task AddSeededGroupRepaymentAsync(ApplicationDbContext context, int fromMemberId, int toMemberId, decimal amount)
    {
        context.Settlements.Add(new Settlement
        {
            GroupId = SplitBillSeeder.GroupId,
            FromMemberId = fromMemberId,
            ToMemberId = toMemberId,
            Amount = amount,
            Date = new DateOnly(2026, 7, 6),
            CreatedByUserId = SplitBillSeeder.OwnerUserId
        });
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 不經移除規則，直接把成員標為已移除
    /// </summary>
    private static async Task RemoveMemberAsync(ApplicationDbContext context, int memberId)
    {
        var member = await context.GroupMembers.SingleAsync(member => member.Id == memberId, Ct);
        context.Remove(member);
        await context.SaveChangesAsync(Ct);
    }

    private const int JpyGroupId = 2;
    private const int JpyMingMemberId = 20;
    private const int AnMemberId = 21;
    private const int ZheMemberId = 22;
    private const int MiaMemberId = 23;

    /// <summary>
    /// 在種子之外再建一個基準幣為 JPY、還沒有任何帳的群組，成員是小明（綁定種子的擁有者）、小安、阿哲與 Mia
    /// </summary>
    /// <remarks>
    /// 小明因此同時在兩個群組裡，種子群組的帳若混進這個群組的計算就會被看出來。
    /// </remarks>
    private static async Task SeedJpyGroupAsync(ApplicationDbContext context)
    {
        context.Groups.Add(new Group
        {
            Id = JpyGroupId,
            OwnerUserId = SplitBillSeeder.OwnerUserId,
            Name = "大阪行",
            Icon = GroupIconType.Travel,
            BaseCurrency = CurrencyType.JPY,
            InviteCode = "OSAKA123"
        });
        context.GroupMembers.AddRange(
            new GroupMember { Id = JpyMingMemberId, GroupId = JpyGroupId, DisplayName = "小明", UserId = SplitBillSeeder.OwnerUserId },
            new GroupMember { Id = AnMemberId, GroupId = JpyGroupId, DisplayName = "小安" },
            new GroupMember { Id = ZheMemberId, GroupId = JpyGroupId, DisplayName = "阿哲" },
            new GroupMember { Id = MiaMemberId, GroupId = JpyGroupId, DisplayName = "Mia" });

        // 建立群組時一定會寫下這一則
        context.ActivityLogs.Add(new ActivityLog
        {
            GroupId = JpyGroupId,
            ActorUserId = SplitBillSeeder.OwnerUserId,
            ActorMemberId = JpyMingMemberId,
            ActorName = "小明",
            ActionType = ActivityActionType.GroupCreated,
            Summary = "建立了群組「大阪行」，基準幣為 JPY"
        });
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 在大阪行補種一筆 JPY 花費：由被欠的一方墊付，只有欠錢的一方一位參與者
    /// </summary>
    private static async Task AddJpyGroupDebtAsync(ApplicationDbContext context, int debtorMemberId, int creditorMemberId, decimal amount)
    {
        context.Expenses.Add(new Expense
        {
            GroupId = JpyGroupId,
            PayerId = creditorMemberId,
            Name = "代墊",
            Category = ExpenseCategoryType.Other,
            Currency = CurrencyType.JPY,
            Amount = amount,
            Rate = 1m,
            Date = new DateOnly(2026, 7, 1),
            CreatedByUserId = SplitBillSeeder.OwnerUserId,
            ExpenseShares = [new ExpenseShare { GroupMemberId = debtorMemberId, Amount = amount }]
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

    [Fact(DisplayName = "建立群組：選定的群組圖示被存入，並出現在回應與單一群組的資料裡")]
    public async Task CreateGroup_WithIcon_StoresIconAndReturnsIt()
    {
        // Arrange - 不用旅行：它是列舉的預設值，圖示沒被存入時照樣會讀到它
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Dining));

        // Assert
        Assert.Equal(GroupIconType.Dining, result.Value!.Icon);
        Assert.Equal(GroupIconType.Dining, (await service.GetGroupByIdAsync(result.Value.Id, MingUserId)).Value!.Icon);
    }

    [Fact(DisplayName = "建立群組：沒帶群組圖示時，請求驗證不通過")]
    public void CreateGroupRequest_MissingIcon_FailsValidation()
    {
        // Arrange - 必填由請求的驗證把關，Controller 在呼叫服務之前就會回 400
        var request = NewGroupRequest();
        request.Icon = null;
        var results = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Equal("請傳入群組圖示", Assert.Single(results).ErrorMessage);
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

    [Fact(DisplayName = "建立群組：User 名稱用到上限時仍能建立，擁有者的顯示名稱等於 User 名稱")]
    public async Task CreateGroup_LongestUserName_OwnerDisplayNameEqualsUserName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        string longestUserName = new('名', MaxLengths.UserName);
        context.Users.Add(new User { Id = MingUserId, Account = "ming", PasswordHash = "not_used", Name = longestUserName });
        await context.SaveChangesAsync(Ct);
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Assert - 顯示名稱取自 User 名稱，User 名稱上限調到超過顯示名稱上限時會存檔失敗
        Assert.Equal(ResultCode.Success, result.Code);
        var member = await context.GroupMembers.SingleAsync(Ct);
        Assert.Equal(longestUserName, member.DisplayName);
    }

    [Fact(DisplayName = "建立群組：群組名稱用到上限時仍能建立，並寫入建立群組的動態")]
    public async Task CreateGroup_LongestName_WritesActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateGroupAsync(MingUserId, NewGroupRequest(new string('名', MaxLengths.GroupName)));

        // Assert
        Assert.Equal(ResultCode.Success, result.Code);
        var log = await context.ActivityLogs.SingleAsync(Ct);
        Assert.Equal(ActivityActionType.GroupCreated, log.ActionType);
    }

    #endregion

    #region 存取檢查

    [Fact(DisplayName = "查詢群組清單：只回傳自己是現役成員的群組")]
    public async Task GetMyGroups_ReturnsOnlyOwnGroups()
    {
        // Arrange - 小明曾是「Amy 的群組」的成員，但已被移除
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        await service.CreateGroupAsync(MingUserId, NewGroupRequest("小明的群組"));
        var amyGroup = await service.CreateGroupAsync(AmyUserId, NewGroupRequest("Amy 的群組"));
        var removedMing = new GroupMember { GroupId = amyGroup.Value!.Id, UserId = MingUserId, DisplayName = "小明" };
        context.GroupMembers.Add(removedMing);
        await context.SaveChangesAsync(Ct);
        await RemoveMemberAsync(context, removedMing.Id);

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
            Date = new DateOnly(2026, 7, 5),
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

    [Fact(DisplayName = "更新群組：換群組圖示寫一則動態，敘述含前後的中文名稱")]
    public async Task UpdateGroup_IconChanged_WritesActivityWithChineseNames()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Travel));

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Icon = GroupIconType.Dining });

        // Assert
        Assert.Equal(GroupIconType.Dining, result.Value!.Icon);

        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct);
        Assert.Equal("修改了群組圖示由「旅行」改為「聚餐」", log.Summary);
    }

    [Fact(DisplayName = "更新群組：同時改名稱與群組圖示，兩項變動記在同一則動態")]
    public async Task UpdateGroup_NameAndIconChanged_WritesOneActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Travel));

        // Act
        await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Name = "週五晚餐", Icon = GroupIconType.Dining });

        // Assert
        var log = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct);
        Assert.Equal("修改了群組名稱由「日本旅遊」改為「週五晚餐」、圖示由「旅行」改為「聚餐」", log.Summary);
    }

    [Fact(DisplayName = "更新群組：送出的群組圖示與現況相同時不寫動態")]
    public async Task UpdateGroup_SameIcon_WritesNoActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Dining));

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Icon = GroupIconType.Dining });

        // Assert - 只剩建立群組那一筆
        Assert.True(result.IsSuccess);
        var log = await context.ActivityLogs.SingleAsync(Ct);
        Assert.Equal(ActivityActionType.GroupCreated, log.ActionType);
    }

    [Fact(DisplayName = "更新群組：沒帶群組圖示時，圖示維持不變")]
    public async Task UpdateGroup_IconOmitted_KeepsIcon()
    {
        // Arrange - 不用旅行：它是列舉的預設值，圖示被清成預設值時照樣會讀到它
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Dining));

        // Act
        var result = await service.UpdateGroupAsync(
            created.Value!.Id, MingUserId, new UpdateGroupRequest { Name = "週五晚餐" });

        // Assert
        Assert.Equal(GroupIconType.Dining, result.Value!.Icon);
        Assert.Equal("修改了群組名稱由「日本旅遊」改為「週五晚餐」", (await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct)).Summary);
    }

    [Fact(DisplayName = "更新群組：名稱與描述用到上限、同時變更基準幣，仍能成功並寫入動態")]
    public async Task UpdateGroup_LongestValuesOnAllFields_WritesActivityLog()
    {
        // Arrange - 舊名稱也用到上限，動態敘述「由…改為…」的兩端才都是最長
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(new string('舊', MaxLengths.GroupName)));

        var request = new UpdateGroupRequest
        {
            Name = new string('新', MaxLengths.GroupName),
            BaseCurrency = CurrencyType.JPY,
            Icon = GroupIconType.Shopping,
            Description = new string('描', MaxLengths.GroupDescription)
        };

        // Act
        var result = await service.UpdateGroupAsync(created.Value!.Id, MingUserId, request);

        // Assert
        Assert.Equal(ResultCode.Success, result.Code);
        Assert.Single(await context.ActivityLogs
            .Where(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated)
            .ToListAsync(Ct));
    }

    #endregion

    #region 擁有者特權

    [Fact(DisplayName = "刪除群組：非成員回 NotFound 而非 Forbidden")]
    public async Task DeleteGroup_NotAMember_ReturnsNotFound()
    {
        // Arrange - 群組存在，但 Amy 不是成員
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.DeleteGroupAsync(created.Value!.Id, AmyUserId);

        // Assert - 存取檢查先於擁有者檢查：非成員若得到 403，就能分辨出這個群組存在
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Single(await context.Groups.ToListAsync(Ct));
    }

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

    #region 群組清單

    [Fact(DisplayName = "群組清單：剛建立的群組帶名稱、群組圖示與基準幣，只有擁有者一位現役成員，已結清且全員結清")]
    public async Task GetMyGroups_NewGroup_ReturnsGroupFieldsAndSettled()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        var created = await service.CreateGroupAsync(MingUserId, NewGroupRequest(icon: GroupIconType.Dining));

        // Act
        var result = await service.GetMyGroupsAsync(MingUserId);

        // Assert
        var item = Assert.Single(result.Value!);
        Assert.Equal(
            (created.Value!.Id, "日本旅遊", GroupIconType.Dining, CurrencyType.TWD, (DateTimeOffset?)null, 1),
            (item.Id, item.Name, item.Icon, item.BaseCurrency, item.ClosedAt, item.ActiveMemberCount));
        Assert.Equal((0m, true, true), (item.PersonalNet, item.IsSettled, item.IsEveryoneSettled));
    }

    [Fact(DisplayName = "群組清單：剛建立的群組，最近一則動態的時間是建立群組那一則")]
    public async Task GetMyGroups_NewGroup_LastActivityAtIsGroupCreation()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedUsersAsync(context);
        var service = CreateService(context);
        await service.CreateGroupAsync(MingUserId, NewGroupRequest());

        // Act
        var result = await service.GetMyGroupsAsync(MingUserId);

        // Assert
        var createdLog = await context.ActivityLogs.SingleAsync(Ct);
        Assert.Equal(createdLog.CreatedAt, Assert.Single(result.Value!).LastActivityAt);
    }

    #endregion

    #region 個人淨額

    [Fact(DisplayName = "個人淨額：別人欠我時為正，跨幣別的分攤以鎖入的 Rate 換算，並扣掉已記錄的還款")]
    public async Task GetMyGroups_OthersOweMe_PersonalNetIsPositive()
    {
        // Arrange - 種子：Amy 欠小明 637.035 − 500 → 137，阿華欠小明 637.035 → 637
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).GetMyGroupsAsync(SplitBillSeeder.OwnerUserId);

        // Assert - 沒乘 Rate 會是 5,500；沒扣還款會是 1,274
        Assert.Equal(774m, Assert.Single(result.Value!).PersonalNet);
    }

    [Fact(DisplayName = "個人淨額：我欠別人時為負")]
    public async Task GetMyGroups_IOweOthers_PersonalNetIsNegative()
    {
        // Arrange - Amy 欠小明 TWD 137
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindSeededAmyAsync(context);

        // Act
        var item = await ReadListItemAsync(context, AmyUserId, SplitBillSeeder.GroupId);

        // Assert
        Assert.Equal(-137m, item.PersonalNet);
    }

    [Fact(DisplayName = "個人淨額：小安欠我 5,000、阿哲欠我 1,000、我欠 Mia 2,000 時為 4,000，別人之間的帳不計入")]
    public async Task GetMyGroups_SeveralBalances_PersonalNetOffsetsOnlyMine()
    {
        // Arrange - 小安與阿哲之間另有一筆帳，跟小明無關
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedJpyGroupAsync(context);
        await AddJpyGroupDebtAsync(context, AnMemberId, JpyMingMemberId, 5000m);
        await AddJpyGroupDebtAsync(context, ZheMemberId, JpyMingMemberId, 1000m);
        await AddJpyGroupDebtAsync(context, JpyMingMemberId, MiaMemberId, 2000m);
        await AddJpyGroupDebtAsync(context, ZheMemberId, AnMemberId, 300m);

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, JpyGroupId);

        // Assert - 混進種子群組的 774 會是 4,774；計入別人之間的帳會差 300
        Assert.Equal(4000m, item.PersonalNet);
    }

    [Fact(DisplayName = "個人淨額：已移除成員參與過的帳照常計入")]
    public async Task GetMyGroups_DebtorRemoved_PersonalNetStillIncludesHisBalance()
    {
        // Arrange - 阿華還欠著小明 TWD 637 就被移除
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, SplitBillSeeder.HuaMemberId);

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, SplitBillSeeder.GroupId);

        // Assert - 經由現役成員查分攤的話，阿華的 637 會消失而剩 137
        Assert.Equal(774m, item.PersonalNet);
    }

    #endregion

    #region 已結清與全員結清

    [Fact(DisplayName = "已結清：小安欠我 600、我欠 Mia 600 時，個人淨額為 0 但尚未結清")]
    public async Task GetMyGroups_PersonalNetZeroButBalancesRemain_NotSettled()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedJpyGroupAsync(context);
        await AddJpyGroupDebtAsync(context, AnMemberId, JpyMingMemberId, 600m);
        await AddJpyGroupDebtAsync(context, JpyMingMemberId, MiaMemberId, 600m);

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, JpyGroupId);

        // Assert - 結算不做最小化轉帳，兩筆都還要處理
        Assert.Equal((0m, false, false), (item.PersonalNet, item.IsSettled, item.IsEveryoneSettled));
    }

    [Fact(DisplayName = "已結清：我跟每個人都清了、但阿華還欠小明時，我已結清，群組不是全員結清")]
    public async Task GetMyGroups_OnlyOthersHaveBalances_SettledButNotEveryone()
    {
        // Arrange - Amy 還清欠小明的 TWD 137，只剩阿華欠小明 TWD 637
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindSeededAmyAsync(context);
        await AddSeededGroupRepaymentAsync(context, SplitBillSeeder.AmyMemberId, SplitBillSeeder.MingMemberId, 137m);

        // Act
        var item = await ReadListItemAsync(context, AmyUserId, SplitBillSeeder.GroupId);

        // Assert
        Assert.Equal((0m, true, false), (item.PersonalNet, item.IsSettled, item.IsEveryoneSettled));
    }

    [Fact(DisplayName = "全員結清：兩兩淨額只剩換算的零頭、捨入後歸 0 時，算已結清且全員結清")]
    public async Task GetMyGroups_BalancesRoundToZero_EveryoneSettled()
    {
        // Arrange - Amy 與阿華照結算的整數還清，各自只剩 0.035 的零頭
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await AddSeededGroupRepaymentAsync(context, SplitBillSeeder.AmyMemberId, SplitBillSeeder.MingMemberId, 137m);
        await AddSeededGroupRepaymentAsync(context, SplitBillSeeder.HuaMemberId, SplitBillSeeder.MingMemberId, 637m);

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, SplitBillSeeder.GroupId);

        // Assert
        Assert.Equal((0m, true, true), (item.PersonalNet, item.IsSettled, item.IsEveryoneSettled));
    }

    #endregion

    #region 成員數、動態時間與已結束

    [Fact(DisplayName = "群組清單：現役成員數不含已移除成員")]
    public async Task GetMyGroups_RemovedMembers_NotCountedInActiveMemberCount()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.FillMembersAsync(context, activeCount: 5, removedCount: 2);

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, SplitBillSeeder.GroupId);

        // Assert
        Assert.Equal(5, item.ActiveMemberCount);
    }

    [Fact(DisplayName = "群組清單：最近一則動態的時間是最新那一則動態的時間")]
    public async Task GetMyGroups_SeveralActivities_LastActivityAtIsNewest()
    {
        // Arrange - 種子已有一則新增花費的動態，改名再寫一則
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await CreateService(context).UpdateGroupAsync(
            SplitBillSeeder.GroupId, SplitBillSeeder.OwnerUserId, new UpdateGroupRequest { Name = "日本旅遊 2026" });

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, SplitBillSeeder.GroupId);

        // Assert
        var renamedLog = await context.ActivityLogs
            .SingleAsync(activityLog => activityLog.ActionType == ActivityActionType.GroupUpdated, Ct);
        Assert.Equal(renamedLog.CreatedAt, item.LastActivityAt);
    }

    [Fact(DisplayName = "群組清單：已結束的群組照樣帶個人淨額、已結清與全員結清")]
    public async Task GetMyGroups_ClosedGroup_StillCarriesBalances()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await CreateService(context).SetGroupClosedAsync(
            SplitBillSeeder.GroupId, SplitBillSeeder.OwnerUserId, new SetGroupClosedRequest { Closed = true });

        // Act
        var item = await ReadListItemAsync(context, SplitBillSeeder.OwnerUserId, SplitBillSeeder.GroupId);

        // Assert
        Assert.NotNull(item.ClosedAt);
        Assert.Equal((774m, false, false), (item.PersonalNet, item.IsSettled, item.IsEveryoneSettled));
    }

    #endregion
}
