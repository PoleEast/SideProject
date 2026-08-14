using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;

namespace Project.Tests.Helpers;

/// <summary>
/// InMemory 資料庫測試辅助類別
/// 每次調用 CreateContext() 都會建立獨立的 InMemory 實例，確保測試互不干涉
/// 後續其他 Service 測試可以直接繼承此類別來復用基礎設定
/// </summary>
public static class DbContextTestHelper
{
    /// <summary>
    /// 建立一個新的 InMemory ApplicationDbContext
    /// 每次調用都使用不同的資料庫名稱，保證測試隔離
    /// </summary>
    /// <param name="dbName">可選的資料庫名稱，未指定時自動生成唯一名稱</param>
    /// <returns>已建立 Schema 的 ApplicationDbContext</returns>
    public static ApplicationDbContext CreateContext(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// 在資料庫中插入一個測試用 User，並將 TransactionBuilder 產生的交易紀錄保存進去
    /// 處理了 Transaction → User 的外鍵關聯和 QueryFilter 需求
    /// </summary>
    /// <param name="context">已建立的 DbContext</param>
    /// <param name="userId">使用者 ID（需與 TransactionBuilder 傳入的 userId 一致）</param>
    /// <param name="transactions">由 TransactionBuilder.Build() 產生的交易清單</param>
    public static async Task SeedTransactionsAsync(ApplicationDbContext context, int userId, List<Transaction> transactions)
    {
        // QueryFilter 要求 User.DeletedAt == null，必須先確保 User 存在
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            context.Users.Add(new User
            {
                Id = userId,
                Account = $"testuser_{userId}",
                PasswordHash = "not_used_in_tests",
                Name = $"測試使用者 {userId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }
}
