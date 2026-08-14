using Project.Data;
using Project.Data.Model;
using Project.Shared.Types;

namespace Project.Tests.Helpers;

/// <summary>
/// Split Bill 測試資料種子
/// </summary>
/// <remarks>
/// Split Bill 的 query filter 串接讓每個測試都得備妥整條上游關聯鏈
/// （User → Group → GroupMember → Expense → ExpenseShare），
/// 因此比照 <see cref="DbContextTestHelper.SeedTransactionsAsync"/> 提供一次建好的輔助方法。
/// </remarks>
public static class SplitBillSeeder
{
    public const int OwnerUserId = 1;
    public const int GroupId = 1;

    /// <summary>小明 - 綁定 OwnerUserId 的成員，同時是唯一一筆花費的付款人</summary>
    public const int MingMemberId = 1;

    /// <summary>Amy - 未綁定 User 的成員（只是一個名字）</summary>
    public const int AmyMemberId = 2;

    /// <summary>阿華 - 未綁定 User 的成員</summary>
    public const int HuaMemberId = 3;

    public const int ExpenseId = 1;

    /// <summary>晚餐花費的原幣總額，由三位成員均分</summary>
    public const decimal ExpenseAmount = 9000m;

    /// <summary>
    /// 建立一個三人群組，含一筆由小明墊付、三人均分的花費
    /// </summary>
    /// <remarks>
    /// 群組基準幣為 TWD，花費原幣為 JPY，因此 Rate 不為 1 —— 讓跨幣別情境也在測試涵蓋內。
    /// </remarks>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        context.Users.Add(new User
        {
            Id = OwnerUserId,
            Account = "ming",
            PasswordHash = "not_used_in_tests",
            Name = "小明"
        });

        context.Groups.Add(new Group
        {
            Id = GroupId,
            OwnerUserId = OwnerUserId,
            Name = "日本旅遊",
            Description = "2026 夏天",
            BaseCurrency = CurrencyType.TWD,
            InviteCode = "ABC12345"
        });

        context.GroupMembers.AddRange(
            new GroupMember { Id = MingMemberId, GroupId = GroupId, DisplayName = "小明", UserId = OwnerUserId },
            new GroupMember { Id = AmyMemberId, GroupId = GroupId, DisplayName = "Amy" },
            new GroupMember { Id = HuaMemberId, GroupId = GroupId, DisplayName = "阿華" });

        context.Expenses.Add(new Expense
        {
            Id = ExpenseId,
            GroupId = GroupId,
            PayerId = MingMemberId,
            Name = "晚餐",
            Category = ExpenseCategoryType.Food,
            Currency = CurrencyType.JPY,
            Amount = ExpenseAmount,
            Rate = 0.212345m,
            Date = new DateTime(2026, 7, 1),
            CreatedByUserId = OwnerUserId
        });

        // 9000 三人均分，加總必須等於 ExpenseAmount
        context.ExpenseShares.AddRange(
            new ExpenseShare { Id = 1, ExpenseId = ExpenseId, GroupMemberId = MingMemberId, Amount = 3000m },
            new ExpenseShare { Id = 2, ExpenseId = ExpenseId, GroupMemberId = AmyMemberId, Amount = 3000m },
            new ExpenseShare { Id = 3, ExpenseId = ExpenseId, GroupMemberId = HuaMemberId, Amount = 3000m });

        context.Settlements.Add(new Settlement
        {
            Id = 1,
            GroupId = GroupId,
            FromMemberId = AmyMemberId,
            ToMemberId = MingMemberId,
            Amount = 500m,
            SettledAt = new DateTime(2026, 7, 5),
            CreatedByUserId = OwnerUserId
        });

        context.ActivityLogs.Add(new ActivityLog
        {
            Id = 1,
            GroupId = GroupId,
            ActorUserId = OwnerUserId,
            ActionType = ActivityActionType.ExpenseCreated,
            TargetExpenseId = ExpenseId,
            Summary = "小明 新增了「晚餐 ¥9,000」，由 3 人均分"
        });

        await context.SaveChangesAsync();

        // 清空追蹤，讓後續的 Remove() 只帶著單一實體進 ChangeTracker。
        // 所有關聯都是 Restrict，若子實體仍被追蹤，EF 會試圖切斷關聯而非串連刪除，
        // 撞上不可為 null 的外鍵而拋錯。實際查詢也只會載入要刪的那一筆。
        context.ChangeTracker.Clear();
    }
}
