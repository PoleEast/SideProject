using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// 群組動態讀取的整合測試
/// 使用 InMemory 資料庫，經由既有的 Service 做動作，再讀取動態觀察結果
/// </summary>
/// <remarks>
/// 種子：小明（擁有者，綁定 User）、Amy、阿華（皆未綁定），邀請碼 ABC12345，
/// 已有一則小明新增花費「晚餐」的動態。
/// </remarks>
public class ActivityServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int OwnerUserId = SplitBillSeeder.OwnerUserId;
    private const int GroupId = SplitBillSeeder.GroupId;
    private const int MingMemberId = SplitBillSeeder.MingMemberId;
    private const int AmyMemberId = SplitBillSeeder.AmyMemberId;
    private const int HuaMemberId = SplitBillSeeder.HuaMemberId;

    /// <summary>真正的 Amy，註冊時填的名字與她在群組裡的顯示名稱不同</summary>
    private const int AmyUserId = 2;
    private const string AmyRegisteredName = "陳怡安";

    /// <summary>持外流邀請碼的陌生人</summary>
    private const int StrangerUserId = 5;
    private const string StrangerRegisteredName = "王大陌";

    /// <summary>真正的阿華</summary>
    private const int HuaUserId = 6;
    private const string HuaRegisteredName = "王小華";

    private static ActivityService CreateService(ApplicationDbContext context) => new(context);

    private static GroupService CreateGroupService(ApplicationDbContext context)
        => new(context, NullLogger<GroupService>.Instance);

    private static SettlementService CreateSettlementService(ApplicationDbContext context)
        => new(context, NullLogger<SettlementService>.Instance);

    private static GroupMemberService CreateMemberService(ApplicationDbContext context)
        => new(context, CreateSettlementService(context), NullLogger<GroupMemberService>.Instance);

    private static InviteService CreateInviteService(ApplicationDbContext context)
        => new(context, NullLogger<InviteService>.Instance);

    private static ExpenseService CreateExpenseService(ApplicationDbContext context)
        => new(
            context,
            new ExchangeRateService(new FakeExchangeRateApiClient(), new MemoryCache(new MemoryCacheOptions())),
            NullLogger<ExpenseService>.Instance);

    private static AddGroupMembersRequest AddRequest(params string[] displayNames) => new() { DisplayNames = [.. displayNames] };

    /// <summary>
    /// 一筆與基準幣同為 TWD 的花費，消費日期早於種子的「晚餐」
    /// </summary>
    private static ExpenseRequest LunchRequest() => new()
    {
        Name = "午餐",
        Description = string.Empty,
        Category = ExpenseCategoryType.Food,
        Currency = CurrencyType.TWD,
        Amount = 900m,
        Date = new DateOnly(2026, 6, 1),
        PayerId = MingMemberId,
        Shares =
        [
            new ExpenseShareRequest { MemberId = MingMemberId, Amount = 300m },
            new ExpenseShareRequest { MemberId = AmyMemberId, Amount = 300m },
            new ExpenseShareRequest { MemberId = HuaMemberId, Amount = 300m }
        ]
    };

    private static async Task<string> CurrentInviteCodeAsync(ApplicationDbContext context)
        => await context.Groups.AsNoTracking()
                               .Where(group => group.Id == GroupId)
                               .Select(group => group.InviteCode)
                               .SingleAsync(Ct);

    /// <summary>
    /// 建立 Amy 的帳號，並讓她認領種子裡的「Amy」
    /// </summary>
    private static async Task ClaimAmyAsync(ApplicationDbContext context)
    {
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, AmyRegisteredName);
        await CreateInviteService(context).ClaimMemberAsync(SplitBillSeeder.InviteCode, AmyMemberId, AmyUserId);
    }

    #region 讀取

    [Fact(DisplayName = "讀取：由新到舊排列")]
    public async Task GetActivities_ReturnsNewestFirst()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));
        await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("阿德"));

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert
        Assert.Equal(
            ["新增了成員「阿德」", "新增了成員「小美」", "新增了花費「晚餐」JPY 9,000，由「小明」付款，3 人分攤"],
            activities.Select(activity => activity.Summary));
    }

    [Fact(DisplayName = "讀取：補記一筆較早日期的花費，它的動態仍排在最前")]
    public async Task GetActivities_BackdatedExpense_IsStillOnTop()
    {
        // Arrange - 午餐的消費日期早於種子的晚餐
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var created = await CreateExpenseService(context).CreateExpenseAsync(GroupId, OwnerUserId, LunchRequest());

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert - 動態照記錄的先後排，不照消費日期
        Assert.Equal(created.Value!.Id, activities.First().TargetExpenseId);
    }

    [Fact(DisplayName = "讀取：花費的動態帶著花費 Id 與記錄時間，與花費無關的動態沒有花費 Id")]
    public async Task GetActivities_ReturnsTargetExpenseAndCreatedAt()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var created = await CreateExpenseService(context).CreateExpenseAsync(GroupId, OwnerUserId, LunchRequest());
        await CreateMemberService(context).AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert
        var memberAdded = activities[0];
        var expenseCreated = activities[1];
        Assert.Equal(ActivityActionType.MemberAdded, memberAdded.ActionType);
        Assert.Null(memberAdded.TargetExpenseId);
        Assert.Empty(memberAdded.Details);
        Assert.Equal(ActivityActionType.ExpenseCreated, expenseCreated.ActionType);
        Assert.Equal(created.Value!.Id, expenseCreated.TargetExpenseId);
        Assert.True(expenseCreated.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal(activities.Count, activities.Select(activity => activity.Id).Distinct().Count());
    }

    [Fact(DisplayName = "讀取：花費刪除後，它的動態仍帶著花費 Id")]
    public async Task GetActivities_DeletedExpense_KeepsTargetExpenseId()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await CreateExpenseService(context).DeleteExpenseAsync(GroupId, SplitBillSeeder.ExpenseId, OwnerUserId);

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert
        Assert.Equal([ActivityActionType.ExpenseDeleted, ActivityActionType.ExpenseCreated], activities.Select(activity => activity.ActionType));
        Assert.All(activities, activity => Assert.Equal(SplitBillSeeder.ExpenseId, activity.TargetExpenseId));
    }

    [Fact(DisplayName = "讀取：非成員回 NotFound")]
    public async Task GetActivities_NotAMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, StrangerUserId, StrangerRegisteredName);

        // Act
        var result = await CreateService(context).GetActivitiesAsync(GroupId, StrangerUserId, limit: null);

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Equal("找不到此群組", result.Message);
    }

    [Fact(DisplayName = "讀取：被解除綁定或被移除後回 NotFound")]
    public async Task GetActivities_AfterUnboundOrRemoved_ReturnsNotFound()
    {
        // Arrange - Amy 認領後被解除綁定；王小華以新成員加入後被移除
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        await SplitBillSeeder.AddUserAsync(context, HuaUserId, HuaRegisteredName);
        await CreateInviteService(context).JoinAsNewMemberAsync(
            SplitBillSeeder.InviteCode, HuaUserId, new JoinAsNewMemberRequest { DisplayName = "華哥" });
        int joinedMemberId = await context.GroupMembers.Where(member => member.UserId == HuaUserId).Select(member => member.Id).SingleAsync(Ct);

        var memberService = CreateMemberService(context);
        await memberService.UnbindMemberAsync(GroupId, AmyMemberId, OwnerUserId);
        await memberService.RemoveMemberAsync(GroupId, joinedMemberId, OwnerUserId);

        // Act
        var unbound = await CreateService(context).GetActivitiesAsync(GroupId, AmyUserId, limit: null);
        var removed = await CreateService(context).GetActivitiesAsync(GroupId, HuaUserId, limit: null);

        // Assert
        Assert.Equal(ResultCode.NotFound, unbound.Code);
        Assert.Equal(ResultCode.NotFound, removed.Code);
    }

    [Fact(DisplayName = "讀取：已結束的群組照常可讀")]
    public async Task GetActivities_ClosedGroup_IsStillReadable()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await CreateGroupService(context).SetGroupClosedAsync(GroupId, OwnerUserId, new SetGroupClosedRequest { Closed = true });

        // Act
        var result = await CreateService(context).GetActivitiesAsync(GroupId, OwnerUserId, limit: null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ActivityActionType.GroupClosed, result.Value!.First().ActionType);
    }

    #endregion

    #region 筆數

    [Fact(DisplayName = "筆數：指定時只取最新的幾則")]
    public async Task GetActivities_WithLimit_ReturnsNewestOnly()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));
        await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("阿德"));

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId, limit: 2);

        // Assert
        Assert.Equal(["新增了成員「阿德」", "新增了成員「小美」"], activities.Select(activity => activity.Summary));
    }

    [Fact(DisplayName = "筆數：超過現有的動態數，或未指定時，回傳全部")]
    public async Task GetActivities_LimitBeyondCountOrOmitted_ReturnsAll()
    {
        // Arrange - 種子一則，加上新增成員一則
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await CreateMemberService(context).AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));

        // Act
        var beyond = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId, limit: 100);
        var omitted = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert
        Assert.Equal(2, beyond.Count);
        Assert.Equal(2, omitted.Count);
    }

    [Theory(DisplayName = "筆數：0 與負數回 ValidationError")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetActivities_NonPositiveLimit_ReturnsValidationError(int limit)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).GetActivitiesAsync(GroupId, OwnerUserId, limit);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
    }

    #endregion

    #region 是不是我

    [Fact(DisplayName = "是不是我：自己做的為 true，別人做的為 false")]
    public async Task GetActivities_MarksOwnActivities()
    {
        // Arrange - Amy 認領後新增了一位成員
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        await CreateMemberService(context).AddMembersAsync(GroupId, AmyUserId, AddRequest("小美"));

        // Act
        var seenByAmy = await SplitBillActivities.ReadSeededGroupAsync(context, AmyUserId);
        var seenByOwner = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);

        // Assert - 由新到舊：Amy 新增成員、Amy 認領、小明新增晚餐
        Assert.Equal([true, true, false], seenByAmy.Select(activity => activity.IsMe));
        Assert.Equal([false, false, true], seenByOwner.Select(activity => activity.IsMe));
    }

    [Fact(DisplayName = "是不是我：解除綁定後改認領另一個位置，先前做的動態仍是我做的")]
    public async Task GetActivities_AfterMovingToAnotherSeat_StillMarksEarlierActivities()
    {
        // Arrange - Amy 先坐「Amy」、新增成員、解除自己的綁定，再以新的邀請碼認領「阿華」
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.AddMembersAsync(GroupId, AmyUserId, AddRequest("小美"));
        await memberService.UnbindMemberAsync(GroupId, AmyMemberId, AmyUserId);
        await CreateInviteService(context).ClaimMemberAsync(await CurrentInviteCodeAsync(context), HuaMemberId, AmyUserId);

        // Act
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, AmyUserId);

        // Assert - 認的是人，不是位置
        var memberAdded = activities.Single(activity => activity.ActionType == ActivityActionType.MemberAdded);
        Assert.True(memberAdded.IsMe);
        Assert.Equal("Amy", memberAdded.ActorName);
    }

    #endregion

    #region 操作者的名稱

    [Fact(DisplayName = "操作者：成員做出的動作，主詞是他在群組裡的顯示名稱")]
    public async Task Activity_ByMember_NamesActorByDisplayName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await CreateMemberService(context).AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));

        // Assert
        var latest = (await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId)).First();
        Assert.Equal(ActivityActionType.MemberAdded, latest.ActionType);
        Assert.Equal("小明", latest.ActorName);
        Assert.Equal("新增了成員「小美」", latest.Summary);
    }

    [Fact(DisplayName = "操作者：成員能做的每一種動作，動態都有主詞")]
    public async Task Activity_EveryMemberAction_HasActorName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        var groupService = CreateGroupService(context);
        var memberService = CreateMemberService(context);
        var expenseService = CreateExpenseService(context);
        var settlementService = CreateSettlementService(context);

        // Act - 全部由小明操作
        await groupService.UpdateGroupAsync(GroupId, OwnerUserId, new UpdateGroupRequest { Name = "東京旅遊" });
        await groupService.SetGroupClosedAsync(GroupId, OwnerUserId, new SetGroupClosedRequest { Closed = true });
        await groupService.SetGroupClosedAsync(GroupId, OwnerUserId, new SetGroupClosedRequest { Closed = false });
        await groupService.ResetInviteCodeAsync(GroupId, OwnerUserId);

        var added = await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));
        int meiMemberId = added.Value!.Single().Id;
        await memberService.RenameMemberAsync(GroupId, meiMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "美美" });
        await memberService.RemoveMemberAsync(GroupId, meiMemberId, OwnerUserId);
        await memberService.UnbindMemberAsync(GroupId, AmyMemberId, OwnerUserId);

        var lunch = await expenseService.CreateExpenseAsync(GroupId, OwnerUserId, LunchRequest());
        var renamedLunch = LunchRequest();
        renamedLunch.Name = "商業午餐";
        await expenseService.UpdateExpenseAsync(GroupId, lunch.Value!.Id, OwnerUserId, renamedLunch);
        await expenseService.DeleteExpenseAsync(GroupId, lunch.Value.Id, OwnerUserId);

        var settlement = await settlementService.RecordSettlementAsync(GroupId, OwnerUserId, new SettlementRequest
        {
            FromMemberId = HuaMemberId,
            ToMemberId = MingMemberId,
            Amount = 100m,
            Date = new DateOnly(2026, 7, 6)
        });
        await settlementService.DeleteSettlementAsync(GroupId, settlement.Value!.Id, OwnerUserId);

        // Assert
        var mine = (await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId)).Where(activity => activity.IsMe).ToList();
        Assert.All(mine, activity => Assert.Equal("小明", activity.ActorName));
        Assert.Equal(
            new[]
            {
                ActivityActionType.GroupUpdated, ActivityActionType.GroupClosed, ActivityActionType.GroupReopened,
                ActivityActionType.InviteCodeReset, ActivityActionType.MemberAdded, ActivityActionType.MemberRenamed,
                ActivityActionType.MemberRemoved, ActivityActionType.MemberUnbound, ActivityActionType.ExpenseCreated,
                ActivityActionType.ExpenseUpdated, ActivityActionType.ExpenseDeleted, ActivityActionType.SettlementRecorded,
                ActivityActionType.SettlementDeleted
            }.Order(),
            mine.Select(activity => activity.ActionType).Distinct().Order());
    }

    [Fact(DisplayName = "操作者：建立群組的動態，主詞是建立者註冊時填的名字")]
    public async Task Activity_GroupCreated_NamesActorByRegisteredName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, AmyRegisteredName);

        // Act
        var created = await CreateGroupService(context).CreateGroupAsync(
            AmyUserId, new CreateGroupRequest { Name = "週五晚餐", BaseCurrency = CurrencyType.TWD });

        // Assert
        var activity = Assert.Single(await SplitBillActivities.ReadAsync(context, created.Value!.Id, AmyUserId));
        Assert.Equal(ActivityActionType.GroupCreated, activity.ActionType);
        Assert.Equal(AmyRegisteredName, activity.ActorName);
        Assert.True(activity.IsMe);
    }

    [Fact(DisplayName = "操作者：認領的動態用註冊時填的名字，之後的動作用群組裡的名字")]
    public async Task Activity_Claim_NamesActorByRegisteredNameThenDisplayName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await ClaimAmyAsync(context);
        await CreateMemberService(context).AddMembersAsync(GroupId, AmyUserId, AddRequest("小美"));

        // Assert
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);
        Assert.Equal(("Amy", "新增了成員「小美」"), (activities[0].ActorName, activities[0].Summary));
        Assert.Equal((AmyRegisteredName, "認領了成員「Amy」"), (activities[1].ActorName, activities[1].Summary));
    }

    [Fact(DisplayName = "操作者：以新成員加入的動態用註冊時填的名字")]
    public async Task Activity_JoinAsNewMember_NamesActorByRegisteredName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, AmyRegisteredName);

        // Act
        await CreateInviteService(context).JoinAsNewMemberAsync(
            SplitBillSeeder.InviteCode, AmyUserId, new JoinAsNewMemberRequest { DisplayName = "Amy Chen" });

        // Assert
        var latest = (await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId)).First();
        Assert.Equal((AmyRegisteredName, "以「Amy Chen」加入了群組"), (latest.ActorName, latest.Summary));
    }

    [Fact(DisplayName = "操作者：改自己的名字時主詞是改名前的名字，先前的動態也不跟著變")]
    public async Task Activity_RenameSelf_NamesActorByNameBeforeRename()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var memberService = CreateMemberService(context);

        // Act
        await memberService.RenameMemberAsync(GroupId, MingMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "明哥" });
        await memberService.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));

        // Assert - 由新到舊：改名後的動作、改名本身、改名前的種子動態
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);
        Assert.Equal(["明哥", "小明", "小明"], activities.Select(activity => activity.ActorName));
        Assert.Equal("將成員「小明」改名為「明哥」", activities[1].Summary);
    }

    [Fact(DisplayName = "操作者：移除自己或解除自己的綁定時，主詞是離開前的名字")]
    public async Task Activity_LeavingGroup_NamesActorByNameBeforeLeaving()
    {
        // Arrange - Amy 認領後解除自己的綁定；小美以新成員加入後移除自己
        const int meiUserId = 7;
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        await SplitBillSeeder.AddUserAsync(context, meiUserId, "林小美");
        var memberService = CreateMemberService(context);

        // Act
        await memberService.UnbindMemberAsync(GroupId, AmyMemberId, AmyUserId);
        await CreateInviteService(context).JoinAsNewMemberAsync(
            await CurrentInviteCodeAsync(context), meiUserId, new JoinAsNewMemberRequest { DisplayName = "小美" });
        int meiMemberId = await context.GroupMembers.Where(member => member.UserId == meiUserId).Select(member => member.Id).SingleAsync(Ct);
        await memberService.RemoveMemberAsync(GroupId, meiMemberId, meiUserId);

        // Assert
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);
        var removed = activities.Single(activity => activity.ActionType == ActivityActionType.MemberRemoved);
        var unbound = activities.Single(activity => activity.ActionType == ActivityActionType.MemberUnbound);
        Assert.Equal("小美", removed.ActorName);
        Assert.Equal("Amy", unbound.ActorName);
    }

    [Fact(DisplayName = "操作者：解除綁定或註銷帳號之後，他先前的動態仍有主詞")]
    public async Task Activity_ActorUnboundOrDeregistered_KeepsActorName()
    {
        // Arrange - Amy 認領後新增了一位成員
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await ClaimAmyAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.AddMembersAsync(GroupId, AmyUserId, AddRequest("小美"));

        // Act
        await memberService.UnbindMemberAsync(GroupId, AmyMemberId, OwnerUserId);
        context.Remove(await context.Users.SingleAsync(user => user.Id == AmyUserId, Ct));
        await context.SaveChangesAsync(Ct);

        // Assert
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);
        var memberAdded = activities.Single(activity => activity.ActionType == ActivityActionType.MemberAdded);
        var claimed = activities.Single(activity => activity.ActionType == ActivityActionType.MemberClaimed);
        Assert.Equal("Amy", memberAdded.ActorName);
        Assert.Equal(AmyRegisteredName, claimed.ActorName);
    }

    [Fact(DisplayName = "操作者：位置換人時，從動態分得出哪一段是誰做的")]
    public async Task Activity_SeatChangesHands_TellsWhoDidWhat()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, StrangerUserId, StrangerRegisteredName);
        await SplitBillSeeder.AddUserAsync(context, HuaUserId, HuaRegisteredName);
        var inviteService = CreateInviteService(context);

        // Act - 陌生人認領「阿華」、刪掉晚餐、被小明解除綁定，之後本人才認領回來
        await inviteService.ClaimMemberAsync(SplitBillSeeder.InviteCode, HuaMemberId, StrangerUserId);
        await CreateExpenseService(context).DeleteExpenseAsync(GroupId, SplitBillSeeder.ExpenseId, StrangerUserId);
        await CreateMemberService(context).UnbindMemberAsync(GroupId, HuaMemberId, OwnerUserId);
        await inviteService.ClaimMemberAsync(await CurrentInviteCodeAsync(context), HuaMemberId, HuaUserId);

        // Assert - 由新到舊
        var activities = await SplitBillActivities.ReadSeededGroupAsync(context, OwnerUserId);
        Assert.Equal(
            [
                (HuaRegisteredName, ActivityActionType.MemberClaimed),
                ("小明", ActivityActionType.MemberUnbound),
                ("阿華", ActivityActionType.ExpenseDeleted),
                (StrangerRegisteredName, ActivityActionType.MemberClaimed),
                ("小明", ActivityActionType.ExpenseCreated)
            ],
            activities.Select(activity => (activity.ActorName, activity.ActionType)));
    }

    #endregion

    #region 備查欄位

    [Fact(DisplayName = "備查：動態記下操作者當時的成員與 User")]
    public async Task Activity_StoresActorMemberAndUser()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, HuaUserId, HuaRegisteredName);
        var inviteService = CreateInviteService(context);

        // Act - 小明新增成員、Amy 認領、王小華以新成員加入
        await CreateMemberService(context).AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));
        await ClaimAmyAsync(context);
        await inviteService.JoinAsNewMemberAsync(SplitBillSeeder.InviteCode, HuaUserId, new JoinAsNewMemberRequest { DisplayName = "華哥" });

        // Assert - 這兩個欄位不對外回傳，只能直接讀資料列
        int joinedMemberId = await context.GroupMembers.Where(member => member.UserId == HuaUserId).Select(member => member.Id).SingleAsync(Ct);
        var stored = await context.ActivityLogs.AsNoTracking()
                                               .OrderBy(activityLog => activityLog.Id)
                                               .Select(activityLog => new { activityLog.ActionType, activityLog.ActorMemberId, activityLog.ActorUserId })
                                               .ToListAsync(Ct);
        Assert.Equal(
            [
                (ActivityActionType.ExpenseCreated, MingMemberId, OwnerUserId),
                (ActivityActionType.MemberAdded, MingMemberId, OwnerUserId),
                (ActivityActionType.MemberClaimed, AmyMemberId, AmyUserId),
                (ActivityActionType.MemberJoined, joinedMemberId, HuaUserId)
            ],
            stored.Select(activityLog => (activityLog.ActionType, activityLog.ActorMemberId, activityLog.ActorUserId)));
    }

    [Fact(DisplayName = "備查：建立與刪除群組的動態，記下擁有者的成員與名稱")]
    public async Task Activity_GroupCreatedAndDeleted_StoresOwnerMember()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, AmyRegisteredName);
        var groupService = CreateGroupService(context);

        // Act
        var created = await groupService.CreateGroupAsync(AmyUserId, new CreateGroupRequest { Name = "週五晚餐", BaseCurrency = CurrencyType.TWD });
        await groupService.DeleteGroupAsync(created.Value!.Id, AmyUserId);

        // Assert - 群組刪除後動態跟著查不到，須略過 query filter 才讀得到
        int ownerMemberId = await context.GroupMembers.IgnoreQueryFilters()
                                                      .Where(member => member.GroupId == created.Value.Id)
                                                      .Select(member => member.Id)
                                                      .SingleAsync(Ct);
        var stored = await context.ActivityLogs.IgnoreQueryFilters()
                                               .AsNoTracking()
                                               .Where(activityLog => activityLog.GroupId == created.Value.Id)
                                               .OrderBy(activityLog => activityLog.Id)
                                               .ToListAsync(Ct);
        Assert.Equal([ActivityActionType.GroupCreated, ActivityActionType.GroupDeleted], stored.Select(activityLog => activityLog.ActionType));
        Assert.All(stored, activityLog => Assert.Equal((ownerMemberId, AmyRegisteredName), (activityLog.ActorMemberId, activityLog.ActorName)));
    }

    #endregion
}
