using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// InviteService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證以邀請碼預覽、認領與以新成員加入
/// </summary>
/// <remarks>
/// 種子：小明（擁有者，綁定 User）、Amy、阿華（皆未綁定），邀請碼 ABC12345。
/// </remarks>
public class InviteServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int GroupId = SplitBillSeeder.GroupId;
    private const string InviteCode = SplitBillSeeder.InviteCode;

    private const int AmyMemberId = SplitBillSeeder.AmyMemberId;

    /// <summary>真正的 Amy，User.Name 與她在群組裡的顯示名稱不同</summary>
    private const int AmyUserId = 2;

    /// <summary>另一位持碼者，用來和 Amy 搶同一個位置</summary>
    private const int HuaUserId = 3;

    private static InviteService CreateService(ApplicationDbContext context)
        => new(context, NullLogger<InviteService>.Instance);

    private static JoinAsNewMemberRequest JoinRequest(string displayName) => new() { DisplayName = displayName };

    private static async Task RemoveHuaAsync(ApplicationDbContext context)
    {
        var hua = await context.GroupMembers.SingleAsync(member => member.Id == SplitBillSeeder.HuaMemberId, Ct);
        context.Remove(hua);
        await context.SaveChangesAsync(Ct);
    }

    #region 預覽

    [Fact(DisplayName = "預覽：非成員看到群組名稱、未移除的成員與綁定狀態，預填名稱為 User.Name")]
    public async Task Preview_NotAMember_ReturnsGroupAndActiveMembers()
    {
        // Arrange - 阿華已被移除
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        await RemoveHuaAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.PreviewAsync(InviteCode, AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
        var preview = result.Value!;
        Assert.Equal(GroupId, preview.GroupId);
        Assert.Equal("日本旅遊", preview.GroupName);
        Assert.Equal("陳怡安", preview.SuggestedDisplayName);
        Assert.Equal(
            [(SplitBillSeeder.MingMemberId, "小明", true), (AmyMemberId, "Amy", false)],
            preview.Members.Select(member => (member.Id, member.DisplayName, member.IsBound)));
        Assert.False(preview.IsMember);
    }

    [Fact(DisplayName = "預覽：已是成員時標示為成員")]
    public async Task Preview_AlreadyMember_ReportsIsMember()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.PreviewAsync(InviteCode, SplitBillSeeder.OwnerUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsMember);
    }

    [Fact(DisplayName = "預覽：不存在的邀請碼與已刪除群組的邀請碼，回 NotFound")]
    public async Task Preview_UnknownCodeOrDeletedGroup_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        var unknown = await service.PreviewAsync("ZZZZ9999", AmyUserId);

        var group = await context.Groups.SingleAsync(storedGroup => storedGroup.Id == GroupId, Ct);
        context.Remove(group);
        await context.SaveChangesAsync(Ct);
        var deleted = await service.PreviewAsync(InviteCode, AmyUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, unknown.Code);
        Assert.Equal(ResultCode.NotFound, deleted.Code);
    }

    [Fact(DisplayName = "預覽：邀請碼打成小寫或前後帶空白，照樣找得到")]
    public async Task Preview_CodeInLowerCaseWithSpaces_FindsGroup()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        var result = await service.PreviewAsync(" abc12345 ", AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(GroupId, result.Value!.GroupId);
    }

    #endregion

    #region 認領

    [Fact(DisplayName = "認領：綁定呼叫者，顯示名稱不變，之後可存取此群組")]
    public async Task ClaimMember_UnboundMember_BindsCallerAndKeepsDisplayName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        var result = await service.ClaimMemberAsync(InviteCode, AmyMemberId, AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(GroupId, result.Value!.Id);

        var amy = await context.GroupMembers.SingleAsync(member => member.Id == AmyMemberId, Ct);
        Assert.Equal(AmyUserId, amy.UserId);
        Assert.Equal("Amy", amy.DisplayName);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(context, AmyUserId));
    }

    [Fact(DisplayName = "認領：寫入動態，操作者是認領者本人")]
    public async Task ClaimMember_LogsClaimByCaller()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        await service.ClaimMemberAsync(InviteCode, AmyMemberId, AmyUserId);

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberClaimed, Ct);
        Assert.Equal(AmyUserId, log.ActorUserId);
        Assert.Equal("認領了成員「Amy」", log.Summary);
    }

    [Fact(DisplayName = "認領：已被綁定的位置回 Conflict，原本的綁定不變")]
    public async Task ClaimMember_BoundMember_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act - 小明的位置綁定著擁有者
        var result = await service.ClaimMemberAsync(InviteCode, SplitBillSeeder.MingMemberId, AmyUserId);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal(ResultCode.NotFound, await SplitBillAccess.ReadSeededGroupAsync(context, AmyUserId));
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(context, SplitBillSeeder.OwnerUserId));
    }

    [Fact(DisplayName = "認領：已被移除的成員與其他群組的成員，回 NotFound")]
    public async Task ClaimMember_RemovedOrOtherGroupMember_ReturnsNotFound()
    {
        // Arrange - 阿華已被移除；週五晚餐另有一個未綁定的位置
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        await RemoveHuaAsync(context);

        var otherGroup = new Group
        {
            OwnerUserId = SplitBillSeeder.OwnerUserId,
            Name = "週五晚餐",
            BaseCurrency = CurrencyType.TWD,
            InviteCode = "FRIDAY01",
            GroupMembers = [new GroupMember { DisplayName = "阿德" }]
        };
        context.Groups.Add(otherGroup);
        await context.SaveChangesAsync(Ct);

        var service = CreateService(context);

        // Act - 都拿日本旅遊的邀請碼
        var removed = await service.ClaimMemberAsync(InviteCode, SplitBillSeeder.HuaMemberId, AmyUserId);
        var otherGroupMember = await service.ClaimMemberAsync(
            InviteCode, otherGroup.GroupMembers.Single().Id, AmyUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, removed.Code);
        Assert.Equal(ResultCode.NotFound, otherGroupMember.Code);
    }

    [Fact(DisplayName = "認領：已結束的群組照樣可以認領")]
    public async Task ClaimMember_ClosedGroup_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");

        var group = await context.Groups.SingleAsync(storedGroup => storedGroup.Id == GroupId, Ct);
        group.ClosedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(Ct);

        var service = CreateService(context);

        // Act
        var result = await service.ClaimMemberAsync(InviteCode, AmyMemberId, AmyUserId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "認領：已是成員的人再認領第二個位置，回 Conflict")]
    public async Task ClaimMember_CallerAlreadyMember_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act - 小明已經綁定自己的位置
        var result = await service.ClaimMemberAsync(InviteCode, AmyMemberId, SplitBillSeeder.OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        var amy = await context.GroupMembers.SingleAsync(member => member.Id == AmyMemberId, Ct);
        Assert.Null(amy.UserId);
    }

    [Fact(DisplayName = "認領：兩人同時認領同一個位置，後到者回 Conflict，位置仍屬先到者")]
    public async Task ClaimMember_ClaimedConcurrently_LaterClaimReturnsConflict()
    {
        // Arrange - 兩個 DbContext 共用同一個資料庫，代表兩個同時進行的請求
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);
        await SplitBillSeeder.AddUserAsync(earlierContext, AmyUserId, "陳怡安");
        await SplitBillSeeder.AddUserAsync(earlierContext, HuaUserId, "王小華");

        // 後到的請求先讀到 Amy 尚未綁定的狀態；同一個 DbContext 之後的查詢會拿回這個已追蹤的實例
        await laterContext.GroupMembers.SingleAsync(member => member.Id == AmyMemberId, Ct);
        await CreateService(earlierContext).ClaimMemberAsync(InviteCode, AmyMemberId, AmyUserId);

        // Act
        var result = await CreateService(laterContext).ClaimMemberAsync(InviteCode, AmyMemberId, HuaUserId);

        // Assert - 「已被綁定」的預先檢查也回 Conflict，以訊息確認這次是存檔時才被擋下
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal("這個位置剛被認領", result.Message);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(earlierContext, AmyUserId));
        Assert.Equal(ResultCode.NotFound, await SplitBillAccess.ReadSeededGroupAsync(earlierContext, HuaUserId));
    }

    #endregion

    #region 以新成員加入

    [Fact(DisplayName = "以新成員加入：建立綁定呼叫者的成員，名稱去除前後空白，之後可存取此群組")]
    public async Task JoinAsNewMember_ValidName_CreatesBoundMember()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        var result = await service.JoinAsNewMemberAsync(InviteCode, AmyUserId, JoinRequest(" Amy Chen "));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(GroupId, result.Value!.Id);

        var joined = await context.GroupMembers.SingleAsync(member => member.UserId == AmyUserId, Ct);
        Assert.Equal("Amy Chen", joined.DisplayName);
        Assert.Equal(ResultCode.Success, await SplitBillAccess.ReadSeededGroupAsync(context, AmyUserId));
    }

    [Fact(DisplayName = "以新成員加入：與現役成員撞名（不分大小寫）回 Conflict，不建立成員")]
    public async Task JoinAsNewMember_NameTakenIgnoringCase_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act - 名單上已有未綁定的「Amy」，這通常表示她該認領那個位置
        var result = await service.JoinAsNewMemberAsync(InviteCode, AmyUserId, JoinRequest("amy"));

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Contains("「amy」", result.Message);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    [Fact(DisplayName = "以新成員加入：已移除成員的名字可以再用")]
    public async Task JoinAsNewMember_NameOfRemovedMember_Succeeds()
    {
        // Arrange - 阿華已被移除
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, HuaUserId, "王小華");
        await RemoveHuaAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.JoinAsNewMemberAsync(InviteCode, HuaUserId, JoinRequest("阿華"));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "以新成員加入：寫入動態，含加入時使用的名字")]
    public async Task JoinAsNewMember_LogsJoinWithDisplayName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        await service.JoinAsNewMemberAsync(InviteCode, AmyUserId, JoinRequest("Amy Chen"));

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.MemberJoined, Ct);
        Assert.Equal(AmyUserId, log.ActorUserId);
        Assert.Equal("以「Amy Chen」加入了群組", log.Summary);
    }

    [Fact(DisplayName = "以新成員加入：已是成員的人回 Conflict，不建立第二個位置")]
    public async Task JoinAsNewMember_CallerAlreadyMember_ReturnsConflict()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.JoinAsNewMemberAsync(InviteCode, SplitBillSeeder.OwnerUserId, JoinRequest("明哥"));

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    [Theory(DisplayName = "以新成員加入：名稱去除空白後為空或超過上限，回 ValidationError")]
    [InlineData("   ")]
    [InlineData("一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一二三")]
    public async Task JoinAsNewMember_InvalidName_ReturnsValidationError(string displayName)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, AmyUserId, "陳怡安");
        var service = CreateService(context);

        // Act
        var result = await service.JoinAsNewMemberAsync(InviteCode, AmyUserId, JoinRequest(displayName));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(SplitBillSeeder.MemberCount, await context.GroupMembers.CountAsync(Ct));
    }

    #endregion
}
