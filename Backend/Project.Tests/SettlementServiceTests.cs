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
/// SettlementService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證結算，以及還款的記錄、列表、刪除與存取檢查
/// </summary>
/// <remarks>
/// 種子：基準幣 TWD，JPY 9,000 由小明付款、三人均分，Rate 0.212345，每人分攤換算為 TWD 637.035；Amy 已還小明 TWD 500。
/// 結算因此是 Amy 欠小明 TWD 137、阿華欠小明 TWD 637。
/// </remarks>
public class SettlementServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int OwnerUserId = SplitBillSeeder.OwnerUserId;
    private const int GroupId = SplitBillSeeder.GroupId;
    private const int MingMemberId = SplitBillSeeder.MingMemberId;
    private const int AmyMemberId = SplitBillSeeder.AmyMemberId;
    private const int HuaMemberId = SplitBillSeeder.HuaMemberId;

    private static SettlementService CreateService(ApplicationDbContext context)
        => new(context, NullLogger<SettlementService>.Instance);

    private static async Task<List<MemberBalance>> ReadBalancesAsync(ApplicationDbContext context)
        => (await CreateService(context).GetBalancesAsync(GroupId, OwnerUserId)).Value!;

    private const int SeededSettlementId = SplitBillSeeder.SettlementId;

    /// <summary>
    /// 不經移除規則，直接把成員標為已移除
    /// </summary>
    private static async Task RemoveMemberAsync(ApplicationDbContext context, int memberId)
    {
        var member = await context.GroupMembers.SingleAsync(member => member.Id == memberId, Ct);
        context.Remove(member);
        await context.SaveChangesAsync(Ct);
    }

    private static readonly DateOnly RepaymentDate = new(2026, 7, 6);

    private static SettlementRequest Repayment(int fromMemberId, int toMemberId, decimal amount) => new()
    {
        FromMemberId = fromMemberId,
        ToMemberId = toMemberId,
        Amount = amount,
        Date = RepaymentDate
    };

    private const int UsdGroupId = 2;
    private const int UsdMingMemberId = 10;
    private const int UsdAmyMemberId = 11;

    /// <summary>
    /// 建立一個基準幣為 USD、還沒有任何帳的群組，成員是小明（綁定擁有者）與 Amy
    /// </summary>
    private static async Task SeedUsdGroupAsync(ApplicationDbContext context)
    {
        context.Groups.Add(new Group
        {
            Id = UsdGroupId,
            OwnerUserId = OwnerUserId,
            Name = "美國出差",
            BaseCurrency = CurrencyType.USD,
            InviteCode = "USD12345"
        });
        context.GroupMembers.AddRange(
            new GroupMember { Id = UsdMingMemberId, GroupId = UsdGroupId, DisplayName = "小明", UserId = OwnerUserId },
            new GroupMember { Id = UsdAmyMemberId, GroupId = UsdGroupId, DisplayName = "Amy" });
        await context.SaveChangesAsync(Ct);
    }

    private const int OtherUserId = 9;
    private const int OtherGroupId = 9;
    private const int OtherGroupMemberId = 50;
    private const int OtherGroupSettlementId = 50;
    private const string NotGroupMemberMessage = "還款人與收款人必須是這個群組的成員";

    /// <summary>
    /// 建立另一個群組，含兩位成員與他們之間的一筆還款；小明不是那個群組的成員
    /// </summary>
    private static async Task SeedOtherGroupAsync(ApplicationDbContext context)
    {
        const int secondMemberId = 51;

        await SplitBillSeeder.AddUserAsync(context, OtherUserId, "路人");
        context.Groups.Add(new Group
        {
            Id = OtherGroupId,
            OwnerUserId = OtherUserId,
            Name = "別人的群組",
            BaseCurrency = CurrencyType.TWD,
            InviteCode = "OTHER123"
        });
        context.GroupMembers.AddRange(
            new GroupMember { Id = OtherGroupMemberId, GroupId = OtherGroupId, DisplayName = "路人", UserId = OtherUserId },
            new GroupMember { Id = secondMemberId, GroupId = OtherGroupId, DisplayName = "路人乙" });
        context.Settlements.Add(new Settlement
        {
            Id = OtherGroupSettlementId,
            GroupId = OtherGroupId,
            FromMemberId = secondMemberId,
            ToMemberId = OtherGroupMemberId,
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            CreatedByUserId = OtherUserId
        });
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 讓 Amy 還清對小明的 TWD 137 後，依正式的移除規則把她移除
    /// </summary>
    /// <returns>Amy 還清時記錄的那筆還款</returns>
    private static async Task<SettlementResponse> SettleAndRemoveAmyAsync(ApplicationDbContext context)
    {
        var service = CreateService(context);
        var settlement = (await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 137m))).Value!;

        var memberService = new GroupMemberService(context, service, NullLogger<GroupMemberService>.Instance);
        Assert.True((await memberService.RemoveMemberAsync(GroupId, AmyMemberId, OwnerUserId)).IsSuccess);

        return settlement;
    }

    #region 結算

    [Fact(DisplayName = "結算：分攤換算成基準幣、扣掉還款後，捨入到台幣整數")]
    public async Task GetBalances_SeededGroup_ConvertsDeductsAndRounds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).GetBalancesAsync(GroupId, OwnerUserId);

        // Assert - Amy：637.035 − 500 = 137.035 → 137；還款方向寫反會變成 1,137
        Assert.True(result.IsSuccess);
        Assert.Equal(
            [
                new MemberBalance(AmyMemberId, MingMemberId, 137m),
                new MemberBalance(HuaMemberId, MingMemberId, 637m)
            ],
            result.Value);
    }

    [Fact(DisplayName = "結算：移除一位成員後，所有兩兩淨額不變")]
    public async Task GetBalances_MemberRemoved_BalancesUnchanged()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, AmyMemberId);

        // Act
        var balances = await ReadBalancesAsync(context);

        // Assert - 經由 GroupMember 導覽屬性查詢的話，Amy 的分攤會整筆消失
        Assert.Equal(
            [
                new MemberBalance(AmyMemberId, MingMemberId, 137m),
                new MemberBalance(HuaMemberId, MingMemberId, 637m)
            ],
            balances);
    }

    [Fact(DisplayName = "結算：刪除花費後只剩還款造成的反向淨額")]
    public async Task GetBalances_ExpenseDeleted_OnlyRepaymentRemains()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var expense = await context.Expenses.FindAsync([SplitBillSeeder.ExpenseId], Ct);
        context.Remove(expense!);
        await context.SaveChangesAsync(Ct);

        // Act
        var balances = await ReadBalancesAsync(context);

        // Assert - Amy 還了小明 500 卻沒有對應的欠款，變成小明欠 Amy
        Assert.Equal([new MemberBalance(MingMemberId, AmyMemberId, 500m)], balances);
    }

    #endregion

    #region 記錄

    [Fact(DisplayName = "記錄：回應含 Id 與送入的內容，部分還款後該組淨額隨之減少")]
    public async Task RecordSettlement_PartialRepayment_ReturnsSettlementAndReducesBalance()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - Amy 欠小明 TWD 137，先還 TWD 100
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 100m));

        // Assert
        Assert.True(result.IsSuccess);
        var settlement = result.Value!;
        Assert.True(settlement.Id > 0);
        Assert.Equal(
            (AmyMemberId, MingMemberId, 100m, RepaymentDate),
            (settlement.FromMemberId, settlement.ToMemberId, settlement.Amount, settlement.Date));
        Assert.Contains(new MemberBalance(AmyMemberId, MingMemberId, 37m), await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "記錄：寫入一筆不帶目標的動態，敘述含還款人、收款人、金額與還款日期")]
    public async Task RecordSettlement_WritesActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 100m));

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.SettlementRecorded, Ct);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Null(log.TargetExpenseId);
        Assert.Equal("記錄了還款：「Amy」還給「小明」TWD 100，還款日期 2026-07-06", log.Summary);
    }

    [Fact(DisplayName = "記錄：基準幣為美元時，動態的金額帶兩位小數")]
    public async Task RecordSettlement_UsdGroup_WritesAmountWithTwoDecimals()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedUsdGroupAsync(context);

        // Act
        await CreateService(context).RecordSettlementAsync(UsdGroupId, OwnerUserId, Repayment(UsdAmyMemberId, UsdMingMemberId, 12.5m));

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.SettlementRecorded, Ct);
        Assert.Equal("記錄了還款：「Amy」還給「小明」USD 12.50，還款日期 2026-07-06", log.Summary);
    }

    [Fact(DisplayName = "記錄：還的錢超過相欠的金額時，方向反轉為對方欠還款人")]
    public async Task RecordSettlement_Overpayment_ReversesBalanceDirection()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - Amy 欠小明 TWD 137.035，卻還了 TWD 200
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 200m));

        // Assert - 多還的 62.965 → 63 變成小明欠 Amy
        Assert.True(result.IsSuccess);
        Assert.Contains(new MemberBalance(MingMemberId, AmyMemberId, 63m), await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "記錄：記錄者不是當事人、兩人之間也沒有相欠時照常可以記錄，收款人因此欠還款人")]
    public async Task RecordSettlement_BetweenTwoOtherMembersWithoutBalance_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - 小明替阿華記下先給 Amy 的 TWD 50；阿華與 Amy 之間原本沒有任何相欠
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(HuaMemberId, AmyMemberId, 50m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(new MemberBalance(AmyMemberId, HuaMemberId, 50m), await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "記錄：已結束的群組照常可以記錄")]
    public async Task RecordSettlement_ClosedGroup_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var group = await context.Groups.SingleAsync(group => group.Id == GroupId, Ct);
        group.ClosedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(Ct);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 100m));

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region 驗證

    [Fact(DisplayName = "驗證：還款人與收款人是同一人時被擋下")]
    public async Task RecordSettlement_SameMemberOnBothSides_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, AmyMemberId, 100m));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("還款人與收款人不可為同一人", result.Message);
    }

    [Theory(DisplayName = "驗證：金額為 0、為負或超過上限時被擋下")]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1_000_000_000)]
    public async Task RecordSettlement_AmountOutOfRange_ReturnsValidationError(int amount)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, amount));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("金額須大於 0，且不超過 999,999,999.99", result.Message);
    }

    [Fact(DisplayName = "驗證：基準幣為台幣時，帶小數的金額被擋下")]
    public async Task RecordSettlement_FractionalAmountInTwdGroup_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 137.5m));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("TWD 的金額必須是整數", result.Message);
    }

    [Fact(DisplayName = "驗證：基準幣為美元時，兩位小數的金額通過")]
    public async Task RecordSettlement_TwoDecimalsInUsdGroup_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedUsdGroupAsync(context);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(UsdGroupId, OwnerUserId, Repayment(UsdAmyMemberId, UsdMingMemberId, 12.34m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(12.34m, result.Value!.Amount);
    }

    [Fact(DisplayName = "驗證：基準幣為美元時，三位小數的金額被擋下")]
    public async Task RecordSettlement_ThreeDecimalsInUsdGroup_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedUsdGroupAsync(context);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(UsdGroupId, OwnerUserId, Repayment(UsdAmyMemberId, UsdMingMemberId, 12.345m));

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("USD 的金額最多 2 位小數", result.Message);
    }

    [Fact(DisplayName = "驗證：還款日期為 UTC 的次日時通過")]
    public async Task RecordSettlement_DateIsUtcTomorrow_Succeeds()
    {
        // Arrange - 比 UTC 快的時區，當地的今天就是 UTC 的明天
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = Repayment(AmyMemberId, MingMemberId, 100m);
        request.Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "驗證：還款日期晚於 UTC 的次日時被擋下")]
    public async Task RecordSettlement_DateBeyondUtcTomorrow_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = Repayment(AmyMemberId, MingMemberId, 100m);
        request.Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("還款日期不可為未來的日期", result.Message);
    }

    [Fact(DisplayName = "驗證：指定其他群組的成員與不存在的成員得到相同的訊息，結算不受影響")]
    public async Task RecordSettlement_MemberOutsideGroup_ReturnsSameValidationError()
    {
        // Arrange
        const int nonexistentMemberId = 999;
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);
        var service = CreateService(context);

        // Act
        var otherGroupResult = await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, OtherGroupMemberId, 100m));
        var nonexistentResult = await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(nonexistentMemberId, MingMemberId, 100m));

        // Assert
        Assert.Equal(ResultCode.ValidationError, otherGroupResult.Code);
        Assert.Equal(ResultCode.ValidationError, nonexistentResult.Code);
        Assert.Equal(NotGroupMemberMessage, otherGroupResult.Message);
        Assert.Equal(NotGroupMemberMessage, nonexistentResult.Message);
        Assert.Equal(
            [
                new MemberBalance(AmyMemberId, MingMemberId, 137m),
                new MemberBalance(HuaMemberId, MingMemberId, 637m)
            ],
            await ReadBalancesAsync(context));
    }

    #endregion

    #region 列表

    [Fact(DisplayName = "列表：每筆還款帶還款人、收款人、金額與還款日期")]
    public async Task GetSettlements_ReturnsSettlementContent()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).GetSettlementsAsync(GroupId, OwnerUserId);

        // Assert - 種子裡 Amy 在 7/5 還了小明 TWD 500
        Assert.True(result.IsSuccess);
        var settlement = Assert.Single(result.Value!);
        Assert.Equal(
            (SeededSettlementId, AmyMemberId, MingMemberId, 500m, new DateOnly(2026, 7, 5)),
            (settlement.Id, settlement.FromMemberId, settlement.ToMemberId, settlement.Amount, settlement.Date));
    }

    [Fact(DisplayName = "列表：依還款日期由新到舊，同日依記錄順序由新到舊")]
    public async Task GetSettlements_OrdersByDateThenIdDescending()
    {
        // Arrange - 種子的 500 在 7/5；10 與 20 都在 7/6，20 較晚記錄；30 是事後補記的 7/3
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 10m));
        await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 20m));
        var backfilledRequest = Repayment(AmyMemberId, MingMemberId, 30m);
        backfilledRequest.Date = new DateOnly(2026, 7, 3);
        await service.RecordSettlementAsync(GroupId, OwnerUserId, backfilledRequest);

        // Act
        var result = await service.GetSettlementsAsync(GroupId, OwnerUserId);

        // Assert
        Assert.Equal([20m, 10m, 500m, 30m], result.Value!.Select(settlement => settlement.Amount));
    }

    [Fact(DisplayName = "列表：兩人的兩兩淨額歸 0 之後，他們的還款仍在列表中")]
    public async Task GetSettlements_PairSettled_StillListsTheirSettlements()
    {
        // Arrange - Amy 欠小明 TWD 137，還清之後這一組不再出現於結算
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        await service.RecordSettlementAsync(GroupId, OwnerUserId, Repayment(AmyMemberId, MingMemberId, 137m));

        // Act
        var result = await service.GetSettlementsAsync(GroupId, OwnerUserId);

        // Assert
        Assert.DoesNotContain(await ReadBalancesAsync(context), balance => balance.DebtorMemberId == AmyMemberId);
        Assert.Equal([137m, 500m], result.Value!.Select(settlement => settlement.Amount));
    }

    [Fact(DisplayName = "列表：還款人被移除後，還款仍在列表中")]
    public async Task GetSettlements_FromMemberRemoved_StillListsSettlement()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, AmyMemberId);

        // Act
        var result = await CreateService(context).GetSettlementsAsync(GroupId, OwnerUserId);

        // Assert
        Assert.Equal(AmyMemberId, Assert.Single(result.Value!).FromMemberId);
    }

    #endregion

    #region 刪除

    [Fact(DisplayName = "刪除：還款不再出現在列表，該組淨額回到還款前的值")]
    public async Task DeleteSettlement_RemovesSettlementFromListAndBalances()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteSettlementAsync(GroupId, SeededSettlementId, OwnerUserId);

        // Assert - 少了已還的 TWD 500，Amy 欠小明的回到 637.035 → 637
        Assert.True(result.IsSuccess);
        Assert.Empty((await service.GetSettlementsAsync(GroupId, OwnerUserId)).Value!);
        Assert.Contains(new MemberBalance(AmyMemberId, MingMemberId, 637m), await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "刪除：寫入一筆不帶目標的動態，敘述含還款原本的內容")]
    public async Task DeleteSettlement_WritesActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await CreateService(context).DeleteSettlementAsync(GroupId, SeededSettlementId, OwnerUserId);

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.SettlementDeleted, Ct);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Null(log.TargetExpenseId);
        Assert.Equal("刪除了還款：「Amy」還給「小明」TWD 500，還款日期 2026-07-05", log.Summary);
    }

    [Fact(DisplayName = "刪除：還款屬於其他群組時回 NotFound，那筆還款仍在")]
    public async Task DeleteSettlement_SettlementOfOtherGroup_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteSettlementAsync(GroupId, OtherGroupSettlementId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Equal("找不到此還款", result.Message);
        Assert.Single((await service.GetSettlementsAsync(OtherGroupId, OtherUserId)).Value!);
    }

    [Fact(DisplayName = "刪除：還款已被刪除時回 NotFound")]
    public async Task DeleteSettlement_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        await service.DeleteSettlementAsync(GroupId, SeededSettlementId, OwnerUserId);

        // Act
        var result = await service.DeleteSettlementAsync(GroupId, SeededSettlementId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
    }

    #endregion

    #region 已移除成員

    [Fact(DisplayName = "已移除成員：可以是還款人，淨額照常被扣抵，動態寫得出他的名字")]
    public async Task RecordSettlement_RemovedMemberAsFromMember_DeductsBalanceAndNamesHim()
    {
        // Arrange - 阿華還欠著小明 TWD 637 就被標為已移除
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, HuaMemberId);

        // Act
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(HuaMemberId, MingMemberId, 637m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(await ReadBalancesAsync(context), balance => balance.DebtorMemberId == HuaMemberId);
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.SettlementRecorded, Ct);
        Assert.Equal("記錄了還款：「阿華」還給「小明」TWD 637，還款日期 2026-07-06", log.Summary);
    }

    [Fact(DisplayName = "已移除成員：可以是收款人")]
    public async Task RecordSettlement_RemovedMemberAsToMember_Succeeds()
    {
        // Arrange - Amy 欠小明 TWD 137.035
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, AmyMemberId);

        // Act - 小明反過來給了 Amy TWD 100
        var result = await CreateService(context).RecordSettlementAsync(GroupId, OwnerUserId, Repayment(MingMemberId, AmyMemberId, 100m));

        // Assert - Amy 欠小明的變成 237.035 → 237
        Assert.True(result.IsSuccess);
        Assert.Contains(new MemberBalance(AmyMemberId, MingMemberId, 237m), await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "已移除成員：還清每一組淨額之後可以被移除")]
    public async Task RecordSettlement_ClearsEveryBalanceOfMember_AllowsRemovingMember()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await SettleAndRemoveAmyAsync(context);

        // Assert - Amy 不再涉及任何一組淨額，只剩阿華欠小明
        Assert.Equal([new MemberBalance(HuaMemberId, MingMemberId, 637m)], await ReadBalancesAsync(context));
    }

    [Fact(DisplayName = "已移除成員：刪除還款使他的淨額再度非 0 時照常成功，淨額出現在結算中")]
    public async Task DeleteSettlement_MakesRemovedMemberBalanceNonZero_SucceedsAndShowsInBalances()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var settlement = await SettleAndRemoveAmyAsync(context);

        // Act
        var result = await CreateService(context).DeleteSettlementAsync(GroupId, settlement.Id, OwnerUserId);

        // Assert - 還清用的 TWD 137 被刪掉，已移除的 Amy 再度欠小明 TWD 137
        Assert.True(result.IsSuccess);
        Assert.Contains(new MemberBalance(AmyMemberId, MingMemberId, 137m), await ReadBalancesAsync(context));
    }

    #endregion

    #region 存取

    [Fact(DisplayName = "存取：非成員呼叫結算、列表、記錄、刪除皆回 NotFound，資料不受影響")]
    public async Task AllOperations_CalledByNonMember_ReturnNotFound()
    {
        // Arrange
        const int strangerUserId = 2;
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, strangerUserId, "路人");
        var service = CreateService(context);

        // Act
        var balancesResult = await service.GetBalancesAsync(GroupId, strangerUserId);
        var listResult = await service.GetSettlementsAsync(GroupId, strangerUserId);
        var recordResult = await service.RecordSettlementAsync(GroupId, strangerUserId, Repayment(AmyMemberId, MingMemberId, 100m));
        var deleteResult = await service.DeleteSettlementAsync(GroupId, SeededSettlementId, strangerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, balancesResult.Code);
        Assert.Equal(ResultCode.NotFound, listResult.Code);
        Assert.Equal(ResultCode.NotFound, recordResult.Code);
        Assert.Equal(ResultCode.NotFound, deleteResult.Code);
        Assert.Equal("找不到此群組", balancesResult.Message);
        Assert.Equal(SeededSettlementId, Assert.Single((await service.GetSettlementsAsync(GroupId, OwnerUserId)).Value!).Id);
    }

    #endregion
}
