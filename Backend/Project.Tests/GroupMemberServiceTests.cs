using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.Constants;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// GroupMemberService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證成員的新增、改名、移除、列表與存取檢查
/// </summary>
/// <remarks>
/// 種子：小明（擁有者，綁定 User）、Amy、阿華（皆未綁定）。JPY 9,000 由小明付款三人均分，
/// Rate 0.212345，每人分攤換算為 TWD 637.035；Amy 已還小明 500。
/// </remarks>
public class GroupMemberServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int OwnerUserId = SplitBillSeeder.OwnerUserId;
    private const int GroupId = SplitBillSeeder.GroupId;

    private static GroupMemberService CreateService(ApplicationDbContext context)
        => new(context, new SettlementService(context), NullLogger<GroupMemberService>.Instance);

    private const int MeiUserId = 2;

    private static AddGroupMembersRequest AddRequest(params string[] displayNames) => new()
    {
        DisplayNames = [.. displayNames]
    };

    private static async Task RemoveAmyAsync(ApplicationDbContext context)
    {
        var amy = await context.GroupMembers.FindAsync([SplitBillSeeder.AmyMemberId], Ct);
        context.Remove(amy!);
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 建立小美的帳號，並讓她綁定一個沒有任何帳的位置
    /// </summary>
    private static async Task<GroupMember> AddMeiAsBoundMemberAsync(ApplicationDbContext context)
    {
        context.Users.Add(new User { Id = MeiUserId, Account = "mei", PasswordHash = "not_used", Name = "小美" });
        var mei = new GroupMember { GroupId = GroupId, DisplayName = "小美", UserId = MeiUserId };
        context.GroupMembers.Add(mei);
        await context.SaveChangesAsync(Ct);

        return mei;
    }

    private const int AmyUserId = 3;

    /// <summary>
    /// 建立 Amy 的帳號，並讓她綁定種子裡那個還有帳沒清的「Amy」位置
    /// </summary>
    private static async Task BindAmyAsync(ApplicationDbContext context)
    {
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var amy = await context.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        amy.UserId = AmyUserId;
        await context.SaveChangesAsync(Ct);
    }

    private static async Task<string> CurrentInviteCodeAsync(ApplicationDbContext context)
        => await context.Groups.AsNoTracking()
            .Where(group => group.Id == GroupId)
            .Select(group => group.InviteCode)
            .SingleAsync(Ct);

    #region 新增

    [Fact(DisplayName = "新增成員：一次加入多位，回傳新成員")]
    public async Task AddMembers_ValidNames_CreatesMembers()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美", "阿德"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["小美", "阿德"], result.Value!.Select(member => member.DisplayName));
        Assert.All(result.Value!, member => Assert.False(member.IsBound));
        Assert.Equal(5, await context.GroupMembers.CountAsync(member => member.GroupId == GroupId, Ct));
    }

    [Fact(DisplayName = "新增成員：一次請求只寫一筆動態，列出所有新成員")]
    public async Task AddMembers_WritesSingleActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美", "阿德"));

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberAdded, Ct);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal("新增了成員「小美」、「阿德」", log.Summary);
    }

    [Fact(DisplayName = "新增成員：與現役成員撞名時回 Conflict，整批都不寫入")]
    public async Task AddMembers_NameTakenByActiveMember_ReturnsConflictAndAddsNothing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美", "Amy"));

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Contains("「Amy」", result.Message);
        Assert.DoesNotContain("小美", result.Message);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
        Assert.False(await context.ActivityLogs.AnyAsync(log => log.ActionType == ActivityActionType.MemberAdded, Ct));
    }

    [Fact(DisplayName = "新增成員：大小寫不同、前後多了空白，仍視為撞名")]
    public async Task AddMembers_NameDiffersOnlyInCaseAndSpaces_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest(" amy "));

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
    }

    [Fact(DisplayName = "新增成員：名稱去除前後空白後才存入")]
    public async Task AddMembers_NameWithSurroundingSpaces_IsStoredTrimmed()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest(" 小美 "));

        // Assert
        var member = Assert.Single(result.Value!);
        Assert.Equal("小美", (await context.GroupMembers.FindAsync([member.Id], Ct))!.DisplayName);
    }

    [Fact(DisplayName = "新增成員：同一批內重複輸入同一個名字，回 Conflict")]
    public async Task AddMembers_DuplicateWithinBatch_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美", "阿德", "小美"));

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Contains("「小美」", result.Message);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    [Fact(DisplayName = "新增成員：已移除成員的名字可以再用")]
    public async Task AddMembers_NameOfRemovedMember_IsReusable()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("Amy"));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "新增成員：一次超過人數上限回 ValidationError")]
    public async Task AddMembers_OverBatchLimit_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        var tooManyNames = Enumerable.Range(1, GroupMemberService.MaxMembersPerBatch + 1)
            .Select(number => $"朋友{number}")
            .ToArray();

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest(tooManyNames));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    [Fact(DisplayName = "新增成員：人數與名字長度都用到上限，動態敘述仍存得進去")]
    public async Task AddMembers_MaxBatchOfLongestNames_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        var longestNames = Enumerable.Range(1, GroupMemberService.MaxMembersPerBatch)
            .Select(number => number.ToString().PadLeft(MaxLengths.GroupMemberDisplayName, '名'))
            .ToArray();

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest(longestNames));

        // Assert - 動態敘述列出每個新成員的名字，調高人數上限會讓它超過 ActivityLog 敘述的上限而存檔失敗
        Assert.Equal(ResultCode.Success, result.Code);
        Assert.Equal(longestNames, result.Value!.Select(member => member.DisplayName));
        Assert.Single(await context.ActivityLogs.Where(log => log.ActionType == ActivityActionType.MemberAdded).ToListAsync(Ct));
    }

    public static TheoryData<string[]> InvalidNameBatches => new()
    {
        Array.Empty<string>(),
        new[] { "小美", "   " },
        new[] { "小美", new string('名', MaxLengths.GroupMemberDisplayName + 1) },
        // JSON 的 ["小美", null] 能通過反序列化與模型驗證 —— nullable 標註擋不住清單裡的 null 元素
        new[] { "小美", null! }
    };

    [Theory(DisplayName = "新增成員：清單為空、名稱為 null、去除空白後為空或超過上限，回 ValidationError")]
    [MemberData(nameof(InvalidNameBatches))]
    public async Task AddMembers_InvalidNames_ReturnsValidationError(string[] displayNames)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest(displayNames));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    #endregion

    #region 改名

    [Fact(DisplayName = "改名：任何成員都能改別人的名字，動態含改名前後")]
    public async Task RenameMember_ValidName_RenamesAndLogsBeforeAndAfter()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "Amy Chen" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Amy Chen", result.Value!.DisplayName);

        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberRenamed, Ct);
        Assert.Equal("將成員「Amy」改名為「Amy Chen」", log.Summary);
    }

    [Fact(DisplayName = "改名：撞到其他現役成員的名字時回 Conflict")]
    public async Task RenameMember_NameTakenByOtherMember_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "阿華" });

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal("Amy", (await context.GroupMembers.FindAsync([SplitBillSeeder.AmyMemberId], Ct))!.DisplayName);
    }

    [Fact(DisplayName = "改名：只改大小寫不算撞到自己")]
    public async Task RenameMember_OnlyCaseChanged_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "AMY" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("AMY", result.Value!.DisplayName);
    }

    [Fact(DisplayName = "改名：改成原本的名字時直接回傳，不寫動態")]
    public async Task RenameMember_SameName_WritesNoActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = " Amy " });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(await context.ActivityLogs.AnyAsync(log => log.ActionType == ActivityActionType.MemberRenamed, Ct));
    }

    public static TheoryData<string> InvalidNames => new()
    {
        "   ",
        new string('名', MaxLengths.GroupMemberDisplayName + 1)
    };

    [Theory(DisplayName = "改名：名稱去除空白後為空或超過上限，回 ValidationError")]
    [MemberData(nameof(InvalidNames))]
    public async Task RenameMember_InvalidName_ReturnsValidationError(string displayName)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = displayName });

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
    }

    [Fact(DisplayName = "改名：已移除的成員回 NotFound")]
    public async Task RenameMember_RemovedMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "Amy Chen" });

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    #endregion

    #region 移除

    [Fact(DisplayName = "移除：擁有者的位置不可移除，連擁有者本人也不行")]
    public async Task RemoveMember_OwnerSeat_ReturnsBusinessRuleViolation()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RemoveMemberAsync(GroupId, SplitBillSeeder.MingMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.NotNull(await context.GroupMembers.FindAsync([SplitBillSeeder.MingMemberId], Ct));
    }

    [Fact(DisplayName = "移除：還有未結清的淨額時不可移除，訊息列出還與他有帳的成員")]
    public async Task RemoveMember_HasUnsettledBalance_ReturnsBusinessRuleViolationNamingCounterpart()
    {
        // Arrange - Amy 還欠小明 137
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RemoveMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Contains("「小明」", result.Message);
        Assert.NotNull(await context.GroupMembers.FindAsync([SplitBillSeeder.AmyMemberId], Ct));
    }

    [Fact(DisplayName = "移除：照畫面金額還清後可移除，換算零頭不會卡住")]
    public async Task RemoveMember_RepaidDisplayedAmount_Succeeds()
    {
        // Arrange - 阿華欠小明 637.035，畫面顯示 637，照還之後剩 0.035
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        context.Settlements.Add(new Settlement
        {
            GroupId = GroupId,
            FromMemberId = SplitBillSeeder.HuaMemberId,
            ToMemberId = SplitBillSeeder.MingMemberId,
            Amount = 637m,
            Date = new DateOnly(2026, 7, 10),
            CreatedByUserId = OwnerUserId
        });
        await context.SaveChangesAsync(Ct);
        var service = CreateService(context);

        // Act
        var result = await service.RemoveMemberAsync(GroupId, SplitBillSeeder.HuaMemberId, OwnerUserId);

        // Assert - 不用 FindAsync：它會直接回傳 change tracker 裡已軟刪的實例，不套 query filter
        Assert.True(result.IsSuccess);
        Assert.False(await context.GroupMembers.AnyAsync(member => member.Id == SplitBillSeeder.HuaMemberId, Ct));
    }

    [Fact(DisplayName = "移除：移除只是名字的成員時寫入「移除了成員」動態，不重置邀請碼")]
    public async Task RemoveMember_UnboundMember_LogsRemovalWithoutReset()
    {
        // Arrange - 小美沒有任何帳
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        var added = await service.AddMembersAsync(GroupId, OwnerUserId, AddRequest("小美"));

        // Act
        var result = await service.RemoveMemberAsync(GroupId, added.Value!.Single().Id, OwnerUserId);

        // Assert
        Assert.True(result.IsSuccess);
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberRemoved, Ct);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal("移除了成員「小美」", log.Summary);
        Assert.Equal(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));
    }

    [Fact(DisplayName = "移除：移除綁定帳號的他人時重置邀請碼，動態說明重置")]
    public async Task RemoveMember_BoundOtherMember_ResetsInviteCode()
    {
        // Arrange - 小美綁定了帳號、沒有任何帳，像是拿外流邀請碼加入的陌生人
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var mei = await AddMeiAsBoundMemberAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RemoveMemberAsync(GroupId, mei.Id, OwnerUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));

        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberRemoved, Ct);
        Assert.Equal("移除了成員「小美」，並重置了邀請碼", log.Summary);
    }

    [Fact(DisplayName = "移除：綁定帳號的成員移除自己即退出，動態寫「退出了群組」、重置邀請碼且之後存取不到")]
    public async Task RemoveMember_Self_LeavesGroupAndLosesAccess()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var mei = await AddMeiAsBoundMemberAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.RemoveMemberAsync(GroupId, mei.Id, MeiUserId);

        // Assert
        Assert.True(result.IsSuccess);

        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberRemoved, Ct);
        Assert.Equal(MeiUserId, log.ActorUserId);
        Assert.Equal("退出了群組，並重置了邀請碼", log.Summary);
        Assert.NotEqual(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));
        Assert.Equal(ResultCode.NotFound, await SplitBillAccess.ReadSeededGroupAsync(context, MeiUserId));
    }

    #endregion

    #region 解除綁定

    [Fact(DisplayName = "解除綁定：解除他人後該 User 失去存取權，位置與帳目保留，邀請碼重置")]
    public async Task UnbindMember_OtherMember_RevokesAccessKeepsSeatAndResetsInviteCode()
    {
        // Arrange - Amy 還欠小明錢，移除不了，只能解除綁定
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultCode.NotFound, await SplitBillAccess.ReadSeededGroupAsync(context, AmyUserId));

        var amy = await context.GroupMembers.AsNoTracking().SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        Assert.Null(amy.UserId);
        Assert.Equal("Amy", amy.DisplayName);
        Assert.True(await context.ExpenseShares.AnyAsync(share => share.GroupMemberId == SplitBillSeeder.AmyMemberId, Ct));

        Assert.NotEqual(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));
    }

    [Fact(DisplayName = "解除綁定：動態寫出被解除的位置，並說明邀請碼已重置")]
    public async Task UnbindMember_OtherMember_LogsUnbindWithReset()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindAmyAsync(context);
        var service = CreateService(context);

        // Act
        await service.UnbindMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId);

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberUnbound, Ct);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal("解除了「Amy」的帳號綁定，並重置了邀請碼", log.Summary);
    }

    [Fact(DisplayName = "解除綁定：擁有者的位置不可解除，連擁有者本人也不行")]
    public async Task UnbindMember_OwnerSeat_ReturnsBusinessRuleViolation()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindAmyAsync(context);
        var service = CreateService(context);

        // Act
        var byOther = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.MingMemberId, AmyUserId);
        var bySelf = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.MingMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.BusinessRuleViolation, byOther.Code);
        Assert.Equal(ResultCode.BusinessRuleViolation, bySelf.Code);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(context, OwnerUserId));
        Assert.Equal(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));
    }

    [Fact(DisplayName = "解除綁定：位置本來就未綁定時直接回成功，不寫動態、不重置邀請碼")]
    public async Task UnbindMember_UnboundSeat_SucceedsWithoutLogOrReset()
    {
        // Arrange - 阿華只是一個名字
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.HuaMemberId, OwnerUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(await context.ActivityLogs.AnyAsync(log => log.ActionType == ActivityActionType.MemberUnbound, Ct));
        Assert.Equal(SplitBillSeeder.InviteCode, await CurrentInviteCodeAsync(context));
    }

    [Fact(DisplayName = "解除綁定：已移除的成員回 NotFound")]
    public async Task UnbindMember_RemovedMember_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    [Fact(DisplayName = "解除綁定：解除自己即退出群組，動態寫出留下的位置名稱")]
    public async Task UnbindMember_Self_LeavesGroupAndLogsSeatName()
    {
        // Arrange - Amy 帳還沒清就想離開
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await BindAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultCode.NotFound, await SplitBillAccess.ReadSeededGroupAsync(context, AmyUserId));

        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberUnbound, Ct);
        Assert.Equal(AmyUserId, log.ActorUserId);
        Assert.Equal("解除了與「Amy」的綁定並退出群組，並重置了邀請碼", log.Summary);
    }

    #endregion

    #region 並行衝突

    // 兩個 DbContext 共用同一個資料庫，代表兩個同時進行的請求。
    // 後到的請求先把位置讀進追蹤；同一個 DbContext 之後的查詢會拿回這個已追蹤的舊實例。

    [Fact(DisplayName = "並行衝突：改名時該位置剛被認領，回 Conflict")]
    public async Task RenameMember_SeatClaimedConcurrently_ReturnsConflict()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);

        await laterContext.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        await BindAmyAsync(earlierContext);

        // Act
        var result = await CreateService(laterContext).RenameMemberAsync(
            GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "Amy Chen" });

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
    }

    [Fact(DisplayName = "並行衝突：移除時該位置剛被認領，回 Conflict，剛加入的人仍可存取")]
    public async Task RemoveMember_SeatClaimedConcurrently_ReturnsConflictAndKeepsNewcomer()
    {
        // Arrange - 小美這個位置沒有任何帳，畫面上看起來只是一個名字
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);

        await SplitBillSeeder.AddUserAsync(earlierContext, MeiUserId, "小美");
        var mei = new GroupMember { GroupId = GroupId, DisplayName = "小美" };
        earlierContext.GroupMembers.Add(mei);
        await earlierContext.SaveChangesAsync(Ct);

        await laterContext.GroupMembers.SingleAsync(member => member.Id == mei.Id, Ct);
        mei.UserId = MeiUserId;
        await earlierContext.SaveChangesAsync(Ct);

        // Act - 依過期的畫面，以為移除的只是一個名字
        var result = await CreateService(laterContext).RemoveMemberAsync(GroupId, mei.Id, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(earlierContext, MeiUserId));
    }

    [Fact(DisplayName = "並行衝突：解除綁定時該位置已換了人，回 Conflict，不會把新主人請出去")]
    public async Task UnbindMember_SeatReclaimedConcurrently_ReturnsConflictAndKeepsNewcomer()
    {
        // Arrange - 陌生人占了 Amy 的位置；兩位成員同時處理，其中一位已先解除綁定、Amy 也已認領回來
        const int strangerUserId = 98;
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);

        await SplitBillSeeder.AddUserAsync(earlierContext, strangerUserId, "路人");
        var seat =await earlierContext.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        seat.UserId = strangerUserId;
        await earlierContext.SaveChangesAsync(Ct);

        await laterContext.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.AmyMemberId, Ct);
        seat.UserId = null;
        await earlierContext.SaveChangesAsync(Ct);
        await BindAmyAsync(earlierContext);

        // Act - 依過期的畫面，以為位置上還是陌生人
        var result = await CreateService(laterContext).UnbindMemberAsync(GroupId, SplitBillSeeder.AmyMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(earlierContext, AmyUserId));
    }

    #endregion

    #region 列表

    [Fact(DisplayName = "列表：包含已移除的成員並加上標記，依 Id 排序")]
    public async Task GetMembers_IncludesRemovedMembersFlagged()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveAmyAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMembersAsync(GroupId, OwnerUserId);

        // Assert - 歷史花費要能顯示「Amy（已移除）」，名字只能從這裡來
        Assert.Equal(
            [("小明", false), ("Amy", true), ("阿華", false)],
            result.Value!.Select(member => (member.DisplayName, member.IsRemoved)));
    }

    [Fact(DisplayName = "列表：「是我」「擁有者」「已綁定」依呼叫者算好")]
    public async Task GetMembers_FlagsDependOnCaller()
    {
        // Arrange - 由非擁有者的小美呼叫，「是我」與「擁有者」才會落在不同成員上
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await AddMeiAsBoundMemberAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMembersAsync(GroupId, MeiUserId);

        // Assert - (IsMe, IsOwner, IsBound)
        Assert.Equal(
            [
                ("小明", false, true, true),
                ("Amy", false, false, false),
                ("阿華", false, false, false),
                ("小美", true, false, true)
            ],
            result.Value!.Select(member => (member.DisplayName, member.IsMe, member.IsOwner, member.IsBound)));
    }

    #endregion

    #region 存取檢查

    [Fact(DisplayName = "存取檢查：非成員呼叫五個端點皆回 NotFound，資料不變")]
    public async Task AllOperations_NotAMember_ReturnNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        const int strangerUserId = 99;
        var service = CreateService(context);

        // Act
        var list = await service.GetMembersAsync(GroupId, strangerUserId);
        var add = await service.AddMembersAsync(GroupId, strangerUserId, AddRequest("陌生人"));
        var rename = await service.RenameMemberAsync(
            GroupId, SplitBillSeeder.HuaMemberId, strangerUserId, new RenameGroupMemberRequest { DisplayName = "被改名" });
        var remove = await service.RemoveMemberAsync(GroupId, SplitBillSeeder.HuaMemberId, strangerUserId);
        var unbind = await service.UnbindMemberAsync(GroupId, SplitBillSeeder.MingMemberId, strangerUserId);

        // Assert - 不區分 404／403，否則能用遞增 id 掃出哪些群組存在
        Assert.All([list.Code, add.Code, rename.Code, remove.Code, unbind.Code], code => Assert.Equal(ResultCode.NotFound, code));
        Assert.Equal(["小明", "Amy", "阿華"], await context.GroupMembers.OrderBy(member => member.Id)
            .Select(member => member.DisplayName).ToListAsync(Ct));
    }

    [Fact(DisplayName = "存取檢查：用自己群組的 id 操作別的群組的成員，回 NotFound")]
    public async Task MemberOperations_MemberOfAnotherGroup_ReturnNotFound()
    {
        // Arrange - 小明另有一個群組，想借它的 id 動到日本旅遊裡的阿華
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var groupService = new GroupService(context, NullLogger<GroupService>.Instance);
        var otherGroup = await groupService.CreateGroupAsync(OwnerUserId, new CreateGroupRequest
        {
            Name = "週五晚餐",
            BaseCurrency = CurrencyType.TWD
        });
        var service = CreateService(context);

        // Act
        var rename = await service.RenameMemberAsync(
            otherGroup.Value!.Id, SplitBillSeeder.HuaMemberId, OwnerUserId, new RenameGroupMemberRequest { DisplayName = "被改名" });
        var remove = await service.RemoveMemberAsync(otherGroup.Value!.Id, SplitBillSeeder.HuaMemberId, OwnerUserId);
        var unbind = await service.UnbindMemberAsync(otherGroup.Value!.Id, SplitBillSeeder.HuaMemberId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, rename.Code);
        Assert.Equal(ResultCode.NotFound, remove.Code);
        Assert.Equal(ResultCode.NotFound, unbind.Code);
    }

    #endregion
}
