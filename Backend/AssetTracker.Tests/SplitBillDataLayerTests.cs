using AssetTracker.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Project.Data.Model;
using Project.Shared.Types;

namespace AssetTracker.Tests;

/// <summary>
/// Split Bill 資料層測試
/// 使用 InMemory 資料庫，驗證軟刪除、query filter 串接與自動時間戳的外部可觀察行為
/// </summary>
/// <remarks>
/// 唯一索引與欄位精度不在此驗證：InMemory provider 不強制唯一約束、也不套用 HasPrecision，
/// 寫出來的測試會恆過，是假的保障。那兩項改由人工檢視 migration 產出的 SQL 把關。
/// </remarks>
public class SplitBillDataLayerTests
{
    /// <summary>
    /// xUnit1051 要求所有可取消的呼叫都帶上測試的 CancellationToken。
    /// 這些測試每個都在毫秒級完成，取消能力沒有實際價值，但滿足 analyzer 的成本很低 ——
    /// 縮寫成一個屬性，讓斷言本身不被樣板文字淹沒。
    /// </summary>
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    #region 軟刪除連鎖

    [Fact(DisplayName = "軟刪群組：所有下游資料都查不到")]
    public async Task SoftDeleteGroup_AllDownstreamDataFilteredOut()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - 軟刪群組
        var group = await context.Groups.SingleAsync(Ct);
        context.Groups.Remove(group);
        await context.SaveChangesAsync(Ct);

        // Assert - 群組本身與五種下游資料全部被 query filter 擋掉
        Assert.Empty(await context.Groups.ToListAsync(Ct));
        Assert.Empty(await context.GroupMembers.ToListAsync(Ct));
        Assert.Empty(await context.Expenses.ToListAsync(Ct));
        Assert.Empty(await context.ExpenseShares.ToListAsync(Ct));
        Assert.Empty(await context.Settlements.ToListAsync(Ct));
        Assert.Empty(await context.ActivityLogs.ToListAsync(Ct));
    }

    [Fact(DisplayName = "軟刪花費：其分攤明細一併查不到")]
    public async Task SoftDeleteExpense_ItsSharesFilteredOut()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var expense = await context.Expenses.SingleAsync(Ct);
        context.Expenses.Remove(expense);
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Empty(await context.Expenses.ToListAsync(Ct));
        Assert.Empty(await context.ExpenseShares.ToListAsync(Ct));
    }

    [Fact(DisplayName = "軟刪使用者：其擁有的群組一併查不到")]
    public async Task SoftDeleteOwnerUser_TheirGroupFilteredOut()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var user = await context.Users.SingleAsync(Ct);
        context.Users.Remove(user);
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Empty(await context.Groups.ToListAsync(Ct));
    }

    #endregion

    #region 成員移除不影響歷史帳目

    [Fact(DisplayName = "移除成員：他的歷史分攤明細仍然保留，花費加總不變")]
    public async Task SoftDeleteGroupMember_HistoricalSharesSurvive()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - 移除 Amy（她在這筆花費裡有 3000 的分攤）
        var amy = await context.GroupMembers.SingleAsync(m => m.Id == SplitBillSeeder.AmyMemberId, Ct);
        context.GroupMembers.Remove(amy);
        await context.SaveChangesAsync(Ct);

        // Assert - 成員清單少一人，但分攤明細一筆都不能少
        Assert.Equal(2, await context.GroupMembers.CountAsync(Ct));

        var shares = await context.ExpenseShares.ToListAsync(Ct);
        Assert.Equal(3, shares.Count);

        // 這是 Split Bill 的核心不變量：明細加總 == 花費原幣總額
        Assert.Equal(SplitBillSeeder.ExpenseAmount, shares.Sum(share => share.Amount));
    }

    [Fact(DisplayName = "移除付款人：他墊付的花費仍然查得到")]
    public async Task SoftDeletePayer_TheirExpenseSurvives()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - 移除小明，他是唯一一筆花費的付款人
        var ming = await context.GroupMembers.SingleAsync(m => m.Id == SplitBillSeeder.MingMemberId, Ct);
        context.GroupMembers.Remove(ming);
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Single(await context.Expenses.ToListAsync(Ct));
    }

    [Fact(DisplayName = "移除成員：他參與的還款紀錄仍然查得到")]
    public async Task SoftDeleteGroupMember_TheirSettlementSurvives()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - Amy 是還款紀錄的付款方
        var amy = await context.GroupMembers.SingleAsync(m => m.Id == SplitBillSeeder.AmyMemberId, Ct);
        context.GroupMembers.Remove(amy);
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Single(await context.Settlements.ToListAsync(Ct));
    }

    #endregion

    #region 軟刪除語意

    [Fact(DisplayName = "刪除操作：填入 DeletedAt 而非真的刪除")]
    public async Task Remove_SetsDeletedAtInsteadOfHardDelete()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var amy = await context.GroupMembers.SingleAsync(m => m.Id == SplitBillSeeder.AmyMemberId, Ct);
        context.GroupMembers.Remove(amy);
        await context.SaveChangesAsync(Ct);

        // Assert - 繞過 query filter 仍撈得到，且 DeletedAt 已填入
        var deleted = await context.GroupMembers
            .IgnoreQueryFilters()
            .SingleAsync(m => m.Id == SplitBillSeeder.AmyMemberId, Ct);

        Assert.NotNull(deleted.DeletedAt);
    }

    #endregion

    #region 自動時間戳

    [Fact(DisplayName = "新增資料：自動填入 CreatedAt 與 UpdatedAt")]
    public async Task Add_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();

        // Act
        await SplitBillSeeder.SeedAsync(context);

        // Assert
        var group = await context.Groups.SingleAsync(Ct);
        Assert.NotEqual(default, group.CreatedAt);
        Assert.NotNull(group.UpdatedAt);
    }

    [Fact(DisplayName = "修改資料：只更新 UpdatedAt，CreatedAt 不動")]
    public async Task Update_OnlyRefreshesUpdatedAt()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        var group = await context.Groups.SingleAsync(Ct);
        var createdAt = group.CreatedAt;
        var updatedAt = group.UpdatedAt;

        // Act
        group.Name = "日本旅遊（改名）";
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Equal(createdAt, group.CreatedAt);
        Assert.NotEqual(updatedAt, group.UpdatedAt);
    }

    [Fact(DisplayName = "群組動態：只有 CreatedAt 也能正常存入，不因缺少其他時間戳而拋錯")]
    public async Task ActivityLog_WithOnlyCreatedAt_SavesWithoutError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        context.ActivityLogs.Add(new ActivityLog
        {
            Id = 2,
            GroupId = SplitBillSeeder.GroupId,
            ActorUserId = SplitBillSeeder.OwnerUserId,
            ActionType = ActivityActionType.SettlementRecorded,
            Summary = "小明 記錄了 Amy 還款 $500"
        });
        await context.SaveChangesAsync(Ct);

        // Assert
        var log = await context.ActivityLogs.SingleAsync(l => l.Id == 2, Ct);
        Assert.NotEqual(default, log.CreatedAt);
    }

    #endregion

    #region 群組生命週期

    [Fact(DisplayName = "標記已結束：群組仍然查得到，也仍能新增花費")]
    public async Task CloseGroup_RemainsQueryableAndWritable()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act - ClosedAt 僅影響首頁分區，不限制任何操作
        var group = await context.Groups.SingleAsync(Ct);
        group.ClosedAt = new DateTime(2026, 7, 10);
        await context.SaveChangesAsync(Ct);

        context.Expenses.Add(new Expense
        {
            Id = 2,
            GroupId = SplitBillSeeder.GroupId,
            PayerId = SplitBillSeeder.HuaMemberId,
            Name = "補記的計程車",
            Category = ExpenseCategoryType.Transport,
            Currency = CurrencyType.JPY,
            Amount = 3000m,
            Rate = 0.212345m,
            Date = new DateTime(2026, 7, 2),
            CreatedByUserId = SplitBillSeeder.OwnerUserId
        });
        await context.SaveChangesAsync(Ct);

        // Assert
        Assert.Single(await context.Groups.ToListAsync(Ct));
        Assert.Equal(2, await context.Expenses.CountAsync(Ct));
    }

    #endregion
}
