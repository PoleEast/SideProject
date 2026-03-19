using AssetTracker.Services;
using AssetTracker.Tests.Helpers;
using Project.Shared.Types;

namespace AssetTracker.Tests;

/// <summary>
/// PositionService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證完整的查詢→計算→回傳流程
/// 每個測試方法都建立獨立的 DbContext 實例，確保完全隔離
/// </summary>
public class PositionServiceTests
{
    #region GetPositionAsync 測試

    [Fact(DisplayName = "持倉查詢：無交易紀錄，回傳空清單")]
    public async Task GetPositionAsync_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        var service = new PositionService(context);

        // Act - userId 999 沒有任何交易
        var result = await service.GetPositionAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact(DisplayName = "持倉查詢：單股多次買入，回傳正確均價與數量")]
    public async Task GetPositionAsync_SingleStockMultipleBuys_ReturnsCorrectPosition()
    {
        // Arrange - 買入 100 股 @ 500, 買入 200 股 @ 600
        // 均價 = (100*500 + 200*600) / 300 = 170000 / 300 ≈ 566.67
        using var context = DbContextTestHelper.CreateContext();
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(100, 500m, new DateTime(2024, 1, 1))
            .Buy(200, 600m, new DateTime(2024, 1, 2))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, transactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetPositionAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);

        var position = result.Value![0];
        Assert.Equal("2330", position.StockCode);
        Assert.Equal(StockMarketType.TW, position.StockMarket);
        Assert.Equal(300, position.Quantity);
        Assert.Equal(566.67m, Math.Round(position.AveragePrice, 2));
    }

    [Fact(DisplayName = "持倉查詢：兩種股票，各自獨立計算持倉")]
    public async Task GetPositionAsync_MultipleStocks_EachCalculatedIndependently()
    {
        // Arrange
        // 2330 台股：買 100 @ 500  → 均價 500, 數量 100
        // AAPL 美股：買 50 @ 150   → 均價 150, 數量 50
        using var context = DbContextTestHelper.CreateContext();

        var twTransactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(100, 500m, new DateTime(2024, 1, 1))
            .Build();

        var usTransactions = new TransactionBuilder("AAPL", StockMarketType.US, userId: 1)
            .Buy(50, 150m, new DateTime(2024, 1, 1))
            .Build();

        // TransactionBuilder 各自從 Id=1 開始計數，插入前需手動修正避免主鍵冲突
        usTransactions[0].Id = 100;

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, twTransactions);
        await DbContextTestHelper.SeedTransactionsAsync(context, 1, usTransactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetPositionAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var tw = result.Value.First(p => p.StockCode == "2330");
        Assert.Equal(StockMarketType.TW, tw.StockMarket);
        Assert.Equal(100, tw.Quantity);
        Assert.Equal(500m, tw.AveragePrice);

        var us = result.Value.First(p => p.StockCode == "AAPL");
        Assert.Equal(StockMarketType.US, us.StockMarket);
        Assert.Equal(50, us.Quantity);
        Assert.Equal(150m, us.AveragePrice);
    }

    [Fact(DisplayName = "持倉查詢：某股票全部賣出，該股票不出現在持倉清單中")]
    public async Task GetPositionAsync_AllSharesSold_StockExcludedFromResult()
    {
        // Arrange
        // 2330：買 100 @ 500, 賣 100 @ 550 → 數量 0，應被排除
        // AAPL：買 50 @ 150              → 數量 50，應出現
        using var context = DbContextTestHelper.CreateContext();

        var twTransactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(100, 500m, new DateTime(2024, 1, 1))
            .Sell(100, 550m, new DateTime(2024, 1, 2))
            .Build();

        var usTransactions = new TransactionBuilder("AAPL", StockMarketType.US, userId: 1)
            .Buy(50, 150m, new DateTime(2024, 1, 1))
            .Build();

        usTransactions[0].Id = 100;

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, twTransactions);
        await DbContextTestHelper.SeedTransactionsAsync(context, 1, usTransactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetPositionAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("AAPL", result.Value![0].StockCode);
    }

    [Fact(DisplayName = "持倉查詢：某股票賣超，驗證失敗回傳 BusinessRuleViolation")]
    public async Task GetPositionAsync_OversoldStock_ReturnsFailure()
    {
        // Arrange - 買 50 股，賣 100 股，淨數量為 -50，觸發驗證失敗
        using var context = DbContextTestHelper.CreateContext();
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(50, 500m, new DateTime(2024, 1, 1))
            .Sell(100, 550m, new DateTime(2024, 1, 2))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, transactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetPositionAsync(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    #endregion

    #region GetRealizedPnLAsync 測試

    [Fact(DisplayName = "已實現損益：無交易紀錄，回傳空清單")]
    public async Task GetRealizedPnLAsync_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        var service = new PositionService(context);

        // Act - userId 999 沒有任何交易
        var result = await service.GetRealizedPnLAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact(DisplayName = "已實現損益：只有買入無賣出，回傳空清單")]
    public async Task GetRealizedPnLAsync_NoBuyOnly_ReturnsEmptyList()
    {
        // Arrange - 只有買入，沒有賣出交易，損益清單應為空
        using var context = DbContextTestHelper.CreateContext();
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(100, 500m, new DateTime(2024, 1, 1))
            .Buy(200, 600m, new DateTime(2024, 1, 2))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, transactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetRealizedPnLAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact(DisplayName = "已實現損益：單股兩次賣出，FIFO 損益均正確計算")]
    public async Task GetRealizedPnLAsync_SingleStockWithSells_ReturnsCorrectPnL()
    {
        // Arrange
        // 買 100 @ 500 (2024-01-01)
        // 買 100 @ 600 (2024-01-02)
        // 賣  60 @ 550 (2024-01-03) → FIFO 消耗第一批 60 股，BuyPrice = 500
        // 賣  80 @ 650 (2024-01-04) → FIFO 消耗第一批剩餘 40 股 @ 500 + 第二批 40 股 @ 600
        //                              BuyPrice = (40*500 + 40*600) / 80 = 44000 / 80 = 550
        using var context = DbContextTestHelper.CreateContext();
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(100, 500m, new DateTime(2024, 1, 1))
            .Buy(100, 600m, new DateTime(2024, 1, 2))
            .Sell(60, 550m, new DateTime(2024, 1, 3))
            .Sell(80, 650m, new DateTime(2024, 1, 4))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, transactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetRealizedPnLAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        // 第一筆賣出：從第一批全消耗，BuyPrice = 500
        var firstSell = result.Value[0];
        Assert.Equal("2330", firstSell.StockCode);
        Assert.Equal(StockMarketType.TW, firstSell.StockMarket);
        Assert.Equal(60, firstSell.SellQuantity);
        Assert.Equal(550m, firstSell.SellPrice);
        Assert.Equal(500m, firstSell.BuyPrice);

        // 第二筆賣出：跨批次，BuyPrice = 550
        var secondSell = result.Value[1];
        Assert.Equal(80, secondSell.SellQuantity);
        Assert.Equal(650m, secondSell.SellPrice);
        Assert.Equal(550m, secondSell.BuyPrice);
    }

    [Fact(DisplayName = "已實現損益：兩種股票各有賣出，合併回傳所有損益")]
    public async Task GetRealizedPnLAsync_MultipleStocksWithSells_ReturnsCombinedPnL()
    {
        // Arrange
        // 2330 台股：買 100 @ 500, 賣 60 @ 550 → BuyPrice = 500
        // AAPL 美股：買 50 @ 150, 賣 30 @ 180 → BuyPrice = 150
        var twTransactions = new TransactionBuilder("2330", StockMarketType.TW, 1)
            .Buy(100, 500m, new DateTime(2025, 1, 1))
            .Sell(60, 550m, new DateTime(2025, 1, 2))
            .Build();

        var usTransactions = new TransactionBuilder("AAPL", StockMarketType.US, 1)
            .Buy(50, 150m, new DateTime(2026, 1, 1))
            .Sell(30, 180m, new DateTime(2026, 1, 2))
            .Build();

        // TransactionBuilder 各自從 Id=1 開始計數，手動調整避免主鍵衝突
        usTransactions[0].Id = 5;
        usTransactions[1].Id = 6;

        using var context = DbContextTestHelper.CreateContext();
        await DbContextTestHelper.SeedTransactionsAsync(context, 1, twTransactions);
        await DbContextTestHelper.SeedTransactionsAsync(context, 1, usTransactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetRealizedPnLAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var tw = result.Value!.First(p => p.StockCode == "2330");
        Assert.Equal(500m, tw.BuyPrice);
        Assert.Equal(550m, tw.SellPrice);
        Assert.Equal(60, tw.SellQuantity);

        var us = result.Value!.First(p => p.StockCode == "AAPL");
        Assert.Equal(150m, us.BuyPrice);
        Assert.Equal(180m, us.SellPrice);
        Assert.Equal(30, us.SellQuantity);
    }

    [Fact(DisplayName = "已實現損益：某股票賣超，驗證失敗回傳 BusinessRuleViolation")]
    public async Task GetRealizedPnLAsync_OversoldStock_ReturnsFailure()
    {
        // Arrange - 買 50 股，賣 100 股，淨數量為 -50，觸發驗證失敗
        using var context = DbContextTestHelper.CreateContext();
        var transactions = new TransactionBuilder("2330", StockMarketType.TW, userId: 1)
            .Buy(50, 500m, new DateTime(2024, 1, 1))
            .Sell(100, 550m, new DateTime(2024, 1, 2))
            .Build();

        await DbContextTestHelper.SeedTransactionsAsync(context, 1, transactions);
        var service = new PositionService(context);

        // Act
        var result = await service.GetRealizedPnLAsync(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    #endregion
}
