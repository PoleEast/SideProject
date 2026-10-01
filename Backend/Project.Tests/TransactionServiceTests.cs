using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Shared.DTOs.Transaction;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// TransactionService 整合測試
/// 使用 InMemory 資料庫，驗證新增、修改、刪除交易時的賣超阻擋
/// </summary>
public class TransactionServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int UserId = 1;
    private const int BuyId = 1;
    private const int SellId = 2;

    private static TransactionService CreateService(ApplicationDbContext context)
        => new(context, NullLogger<TransactionService>.Instance);

    /// <summary>
    /// 2330：1/1 買入 100 股（Id 1），1/2 賣出 80 股（Id 2），持有 20 股
    /// </summary>
    private static async Task SeedBuyThenSellAsync(ApplicationDbContext context)
    {
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, UserId)
            .Buy(100, 500m, new DateOnly(2024, 1, 1))
            .Sell(80, 550m, new DateOnly(2024, 1, 2))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, UserId, transactions);
    }

    /// <summary>
    /// 2330：1/1 買入 50 股，1/2 與 1/3 各賣出 80 股，為阻擋功能上線前就已賣超的資料
    /// </summary>
    private static async Task SeedOversoldAsync(ApplicationDbContext context)
    {
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, UserId)
            .Buy(50, 500m, new DateOnly(2024, 1, 1))
            .Sell(80, 550m, new DateOnly(2024, 1, 2))
            .Sell(80, 560m, new DateOnly(2024, 1, 3))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, UserId, transactions);
    }

    private static CreateTransactionRequest NewRequest(TransactionType type, int quantity, DateOnly date) => new()
    {
        StockCode = "2330",
        Market = StockMarketType.TW,
        Date = date,
        Type = type,
        Price = 600m,
        Quantity = quantity
    };

    #region CreateTransactionAsync 測試

    [Fact(DisplayName = "新增交易：賣出超過持股，回傳 BusinessRuleViolation 且不寫入")]
    public async Task CreateTransactionAsync_SellExceedsHolding_ReturnsFailureAndSavesNothing()
    {
        // Arrange - 持有 20 股
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act - 再賣 21 股
        var result = await service.CreateTransactionAsync(UserId, NewRequest(TransactionType.Sell, 21, new DateOnly(2024, 1, 3)));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Equal(2, await context.Transactions.CountAsync(Ct));
    }

    [Fact(DisplayName = "新增交易：賣出恰好等於持股，寫入成功")]
    public async Task CreateTransactionAsync_SellEqualsHolding_Succeeds()
    {
        // Arrange - 持有 20 股
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateTransactionAsync(UserId, NewRequest(TransactionType.Sell, 20, new DateOnly(2024, 1, 3)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, await context.Transactions.CountAsync(Ct));
    }

    [Fact(DisplayName = "新增交易：賣出日期早於買入，回傳 BusinessRuleViolation")]
    public async Task CreateTransactionAsync_SellDatedBeforeBuy_ReturnsFailure()
    {
        // Arrange - 1/1 才買入 100 股
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act - 補登一筆 2023/12/31 的賣出，當時還沒有持股
        var result = await service.CreateTransactionAsync(UserId, NewRequest(TransactionType.Sell, 10, new DateOnly(2023, 12, 31)));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    [Fact(DisplayName = "新增交易：既有資料已賣超，仍可新增買入")]
    public async Task CreateTransactionAsync_BuyOnOversoldStock_Succeeds()
    {
        // Arrange - 買入只會增加持股，補不滿缺口也要能寫入，使用者才能逐筆修正
        using var context = DbContextTestHelper.CreateContext();
        await SeedOversoldAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CreateTransactionAsync(UserId, NewRequest(TransactionType.Buy, 10, new DateOnly(2024, 1, 1)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, await context.Transactions.CountAsync(Ct));
    }

    #endregion

    #region UpdateTransactionAsync 測試

    [Fact(DisplayName = "修改交易：買入數量改到低於已賣出，回傳 BusinessRuleViolation 且不寫入")]
    public async Task UpdateTransactionAsync_BuyReducedBelowSold_ReturnsFailureAndSavesNothing()
    {
        // Arrange - 買入 100、賣出 80
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act - 買入改成 79 股
        var result = await service.UpdateTransactionAsync(BuyId, UserId, new UpdateTransactionRequest { Quantity = 79 });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);

        var stored = await context.Transactions.AsNoTracking().SingleAsync(transaction => transaction.Id == BuyId, Ct);
        Assert.Equal(100, stored.Quantity);
    }

    [Fact(DisplayName = "修改交易：買入改成另一檔股票，原股票變成賣超，回傳 BusinessRuleViolation")]
    public async Task UpdateTransactionAsync_BuyMovedToAnotherStock_ReturnsFailure()
    {
        // Arrange - 2330 買入 100、賣出 80
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act - 買入改記到 2317，2330 只剩賣出 80
        var result = await service.UpdateTransactionAsync(BuyId, UserId, new UpdateTransactionRequest { StockCode = "2317" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Contains("2330", result.Message);
    }

    [Fact(DisplayName = "修改交易：賣出數量改到等於買入，寫入成功")]
    public async Task UpdateTransactionAsync_SellIncreasedToHolding_Succeeds()
    {
        // Arrange - 買入 100、賣出 80
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act - 賣出改成 100 股
        var result = await service.UpdateTransactionAsync(SellId, UserId, new UpdateTransactionRequest { Quantity = 100 });

        // Assert
        Assert.True(result.IsSuccess);

        var stored = await context.Transactions.AsNoTracking().SingleAsync(transaction => transaction.Id == SellId, Ct);
        Assert.Equal(100, stored.Quantity);
    }

    #endregion

    #region DeleteTransactionAsync 測試

    [Fact(DisplayName = "刪除交易：買入已被賣出使用，回傳 BusinessRuleViolation 且不刪除")]
    public async Task DeleteTransactionAsync_BuyNeededBySell_ReturnsFailureAndKeepsTransaction()
    {
        // Arrange - 買入 100、賣出 80
        using var context = DbContextTestHelper.CreateContext();
        await SeedBuyThenSellAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteTransactionAsync(BuyId, UserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
        Assert.Equal(2, await context.Transactions.CountAsync(Ct));
    }

    [Fact(DisplayName = "刪除交易：既有資料已賣超，仍可刪除賣出")]
    public async Task DeleteTransactionAsync_SellOnOversoldStock_Succeeds()
    {
        // Arrange - 買入 50、賣出 80 兩筆，刪掉一筆後仍賣超 30 股
        using var context = DbContextTestHelper.CreateContext();
        await SeedOversoldAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteTransactionAsync(SellId, UserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await context.Transactions.CountAsync(Ct));
    }

    #endregion
}
