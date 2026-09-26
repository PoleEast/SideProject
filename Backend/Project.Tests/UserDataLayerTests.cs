using Microsoft.EntityFrameworkCore;
using Project.Data.Model;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// 使用者資料層測試
/// 使用 InMemory 資料庫，驗證使用者與其交易、頭像之間的刪除行為
/// </summary>
public class UserDataLayerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int UserId = 1;

    [Fact(DisplayName = "軟刪使用者：交易與頭像正被追蹤時，資料原封不動保留")]
    public async Task SoftDeleteUser_TrackedTransactionsAndAvatarsSurvive()
    {
        // Arrange - 種子資料不清空追蹤，等同同一個請求先前已載入這些子資料
        using var context = DbContextTestHelper.CreateContext();

        var transactions = new TransactionBuilder(userId: UserId)
            .Buy(1000, 500m)
            .Sell(500, 600m)
            .Build();
        await DbContextTestHelper.SeedTransactionsAsync(context, UserId, transactions);

        context.Avatars.Add(new Avatar { Id = 1, UserId = UserId, PublicId = "avatar_1", Type = AvatarType.custom, IsCurrent = true });
        await context.SaveChangesAsync(Ct);

        var user = await context.Users.SingleAsync(Ct);

        // Act
        context.Users.Remove(user);
        await context.SaveChangesAsync(Ct);

        // Assert - 清空追蹤後重新讀取，確認的是資料庫裡的值
        context.ChangeTracker.Clear();

        Assert.Empty(await context.Transactions.ToListAsync(Ct));

        // 頭像沒有 DeletedAt，被刪就是真的從資料庫消失
        Assert.Single(await context.Avatars.IgnoreQueryFilters().ToListAsync(Ct));

        // 交易的 DeletedAt 只記錄使用者親手刪除的那幾筆，不因帳號註銷被一併蓋上
        var storedTransactions = await context.Transactions.IgnoreQueryFilters().ToListAsync(Ct);
        Assert.Equal(2, storedTransactions.Count);
        Assert.All(storedTransactions, transaction => Assert.Null(transaction.DeletedAt));
    }

    [Fact(DisplayName = "從導覽集合移除交易：拋出例外，資料不被刪除")]
    public async Task RemoveTransactionFromUserCollection_ThrowsAndKeepsData()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();

        var transactions = new TransactionBuilder(userId: UserId).Buy(1000, 500m).Build();
        await DbContextTestHelper.SeedTransactionsAsync(context, UserId, transactions);

        var user = await context.Users.Include(u => u.Transactions).SingleAsync(Ct);

        // Act - 從集合拿掉等於切斷必要關聯，EF 不替它決定是否刪除
        user.Transactions.Remove(user.Transactions.Single());

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(Ct));

        context.ChangeTracker.Clear();
        var stored = await context.Transactions.SingleAsync(Ct);
        Assert.Null(stored.DeletedAt);
    }
}
