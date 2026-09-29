using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs.SplitBill;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// SettlementService 整合測試
/// 使用 InMemory 資料庫，驗證從分攤與還款紀錄推導出捨入後兩兩淨額的完整路徑
/// </summary>
/// <remarks>
/// 種子：基準幣 TWD，¥9,000 由小明付款、三人均分，Rate 0.212345，每人分攤換算為 637.035；Amy 已還小明 500。
/// </remarks>
public class SettlementServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SettlementService CreateService(ApplicationDbContext context) => new(context);

    private static async Task<Group> FindSeededGroupAsync(ApplicationDbContext context)
        => (await context.Groups.FindAsync([SplitBillSeeder.GroupId], Ct))!;

    [Fact(DisplayName = "淨額：分攤換算成基準幣、扣掉還款後，捨入到台幣整數")]
    public async Task GetBalances_SeededGroup_ConvertsDeductsAndRounds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetBalancesAsync(await FindSeededGroupAsync(context));

        // Assert - Amy：637.035 − 500 = 137.035 → 137；還款方向寫反會變成 1,137
        Assert.Equal(
            [
                new MemberBalance(SplitBillSeeder.AmyMemberId, SplitBillSeeder.MingMemberId, 137m),
                new MemberBalance(SplitBillSeeder.HuaMemberId, SplitBillSeeder.MingMemberId, 637m)
            ],
            result);
    }

    [Fact(DisplayName = "淨額：移除一位成員後，所有兩兩淨額不變")]
    public async Task GetBalances_MemberRemoved_BalancesUnchanged()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var amy = await context.GroupMembers.FindAsync([SplitBillSeeder.AmyMemberId], Ct);
        context.Remove(amy!);
        await context.SaveChangesAsync(Ct);
        var service = CreateService(context);

        // Act
        var result = await service.GetBalancesAsync(await FindSeededGroupAsync(context));

        // Assert - 經由 GroupMember 導覽屬性查詢的話，Amy 的分攤會整筆消失
        Assert.Equal(
            [
                new MemberBalance(SplitBillSeeder.AmyMemberId, SplitBillSeeder.MingMemberId, 137m),
                new MemberBalance(SplitBillSeeder.HuaMemberId, SplitBillSeeder.MingMemberId, 637m)
            ],
            result);
    }

    [Fact(DisplayName = "淨額：刪除花費後只剩還款造成的反向淨額")]
    public async Task GetBalances_ExpenseDeleted_OnlyRepaymentRemains()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var expense = await context.Expenses.FindAsync([SplitBillSeeder.ExpenseId], Ct);
        context.Remove(expense!);
        await context.SaveChangesAsync(Ct);
        var service = CreateService(context);

        // Act
        var result = await service.GetBalancesAsync(await FindSeededGroupAsync(context));

        // Assert - Amy 還了小明 500 卻沒有對應的欠款，變成小明欠 Amy
        Assert.Equal(
            [new MemberBalance(SplitBillSeeder.MingMemberId, SplitBillSeeder.AmyMemberId, 500m)],
            result);
    }
}
