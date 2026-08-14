using Project.Tests.Helpers;
using Project.Api.Helpers;
using Project.Data.Model;
using Project.Shared.Types;

namespace Project.Tests;

/// <summary>
/// PositionCalculator 單元測試
/// 測試 FIFO 先進先出法的成本與損益計算邏輯
/// </summary>
public class PositionCalculatorTests
{
    #region CalculateTradeShares 測試

    [Fact(DisplayName = "計算持股數：只有買入，回傳總數量")]
    public void CalculateTradeShares_OnlyBuys_ReturnsTotal()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Buy(200, 20m)
            .Build();

        // Act
        var result = PositionCalculator.CalculateTradeShares(transactions);

        // Assert
        Assert.Equal(300, result);
    }

    [Fact(DisplayName = "計算持股數：有買有賣，回傳淨數量")]
    public void CalculateTradeShares_BuysAndSells_ReturnsNetQuantity()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Buy(200, 20m)
            .Sell(50, 15m)
            .Build();

        // Act
        var result = PositionCalculator.CalculateTradeShares(transactions);

        // Assert
        Assert.Equal(250, result);
    }

    [Fact(DisplayName = "計算持股數：全部賣出，回傳零")]
    public void CalculateTradeShares_AllSold_ReturnsZero()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Sell(100, 15m)
            .Build();

        // Act
        var result = PositionCalculator.CalculateTradeShares(transactions);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact(DisplayName = "計算持股數：空清單，回傳零")]
    public void CalculateTradeShares_EmptyList_ReturnsZero()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var result = PositionCalculator.CalculateTradeShares(transactions);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region ValidateTransactions 測試

    [Fact(DisplayName = "驗證交易：合法交易，回傳成功")]
    public void ValidateTransactions_ValidTransactions_ReturnsSuccess()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Sell(50, 15m)
            .Build();

        // Act
        var result = PositionCalculator.ValidateTransactions(transactions);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "驗證交易：賣超，回傳失敗")]
    public void ValidateTransactions_SellExceedsBuy_ReturnsFailure()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Sell(150, 15m)
            .Build();

        // Act
        var result = PositionCalculator.ValidateTransactions(transactions);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    [Fact(DisplayName = "驗證交易：賣出恰好等於買入，淨數量為零，回傳成功")]
    public void ValidateTransactions_SellExactlyEqualsBuy_ReturnsSuccess()
    {
        // Arrange - 淨數量 = 100 - 100 = 0，不構成賣超
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Sell(100, 15m)
            .Build();

        // Act
        var result = PositionCalculator.ValidateTransactions(transactions);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "驗證交易：兩種股票混合，其中一種賣超，回傳失敗")]
    public void ValidateTransactions_MixedStocks_OneOversold_ReturnsFailure()
    {
        // Arrange - 2330 合法（買 100 賣 50），AAPL 賣超（買 30 賣 50）
        var twTransactions = new TransactionBuilder("2330", StockMarketType.TW)
            .Buy(100, 500m)
            .Sell(50, 550m)
            .Build();

        var usTransactions = new TransactionBuilder("AAPL", StockMarketType.US)
            .Buy(30, 150m)
            .Sell(50, 160m)
            .Build();

        var combined = twTransactions.Concat(usTransactions);

        // Act
        var result = PositionCalculator.ValidateTransactions(combined);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    [Fact(DisplayName = "驗證交易：空清單，回傳成功")]
    public void ValidateTransactions_EmptyList_ReturnsSuccess()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var result = PositionCalculator.ValidateTransactions(transactions);

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region CalculateAverageCost 測試

    [Fact(DisplayName = "平均成本：只有買入，回傳加權平均")]
    public void CalculateAverageCost_OnlyBuys_ReturnsWeightedAverage()
    {
        // Arrange - 買 100 股 @ $10, 買 100 股 @ $20
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2024, 1, 1))
            .Buy(100, 20m, new DateTime(2024, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateAverageCost(transactions);

        // Assert - 平均成本 = (100*10 + 100*20) / 200 = 15
        Assert.Equal(15m, result);
    }

    [Fact(DisplayName = "平均成本：全部賣出，回傳零")]
    public void CalculateAverageCost_AllSold_ReturnsZero()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2024, 1, 1))
            .Sell(100, 15m, new DateTime(2024, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateAverageCost(transactions);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact(DisplayName = "平均成本：部分賣出 FIFO，回傳正確均價")]
    public void CalculateAverageCost_PartialSellFIFO_ReturnsCorrectAverage()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2025, 1, 1))
            .Buy(100, 20m, new DateTime(2026, 1, 2))
            .Sell(50, 15m, new DateTime(2026, 1, 20))
            .Build();

        // Act
        var result = PositionCalculator.CalculateAverageCost(transactions);

        // Assert
        Assert.Equal(16.67m, Math.Round(result, 2));
    }

    [Fact(DisplayName = "平均成本：賣出數量恰好等於第一筆買入數量，FIFO 完全消耗第一批，均價等於第二批價格")]
    public void CalculateAverageCost_SellExactlyFirstLot_ReturnsSecondLotPrice()
    {
        // Arrange - 買 100@10, 買 100@20, 賣 100
        // FIFO：賣出數量 100 恰好等於第一批，第一批被完全消耗
        // 剩餘持倉全部來自第二批，均價 = 20
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2024, 1, 1))
            .Buy(100, 20m, new DateTime(2024, 1, 2))
            .Sell(100, 15m, new DateTime(2024, 1, 3))
            .Build();

        // Act
        var result = PositionCalculator.CalculateAverageCost(transactions);

        // Assert
        Assert.Equal(20m, result);
    }

    [Fact(DisplayName = "平均成本：空清單，回傳零")]
    public void CalculateAverageCost_EmptyList_ReturnsZero()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var result = PositionCalculator.CalculateAverageCost(transactions);

        // Assert
        Assert.Equal(0m, result);
    }

    #endregion

    #region CalculateRealizedPnL 測試

    [Fact(DisplayName = "已實現損益：無賣出，回傳空清單")]
    public void CalculateRealizedPnL_NoSells_ReturnsEmptyList()
    {
        // Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m)
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact(DisplayName = "已實現損益：單筆買賣，回傳正確損益")]
    public void CalculateRealizedPnL_SimpleBuySell_ReturnsCorrectPnL()
    {
        // Arrange - 買 100 股 @ $10, 賣 100 股 @ $15
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2024, 1, 1))
            .Sell(100, 15m, new DateTime(2024, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);

        var pnl = result.Value![0];
        Assert.Equal(10m, pnl.BuyPrice);   // 買入均價
        Assert.Equal(15m, pnl.SellPrice);  // 賣出價格
        Assert.Equal(100, pnl.SellQuantity);
    }

    [Fact(DisplayName = "已實現損益：多次買入單次賣出 FIFO，回傳正確買入均價")]
    public void CalculateRealizedPnL_MultipleBuysSingleSellFIFO_ReturnsCorrectBuyPrice()
    {
        //Arrange
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2025, 1, 1))
            .Buy(100, 20m, new DateTime(2025, 1, 2))
            .Sell(150, 18m, new DateTime(2025, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(13.33m, Math.Round(result.Value![0].BuyPrice, 2));
        Assert.Equal(18m, result.Value![0].SellPrice);
        Assert.Equal(150, result.Value![0].SellQuantity);
    }

    [Fact(DisplayName = "已實現損益：多次賣出，第二筆賣出攜帶前一筆的剩餘買入數量，損益計算正確")]
    public void CalculateRealizedPnL_MultipleSells_RemainingQuantityCarriedOver_ReturnsCorrectPnL()
    {
        // Arrange - 買 100@10, 買 100@20, 賣 60@15, 賣 80@18
        // 第一筆賣出 60：從第一批(100@10)消耗 60 股，剩餘 40 股@10
        //   BuyPrice = (60 * 10) / 60 = 10
        // 第二筆賣出 80：先消耗攜帶過來的 40 股@10，再從第二批(100@20)消耗 40 股
        //   BuyPrice = (40*10 + 40*20) / 80 = 1200 / 80 = 15
        var transactions = new TransactionBuilder()
            .Buy(100, 10m, new DateTime(2024, 1, 1))
            .Buy(100, 20m, new DateTime(2024, 1, 2))
            .Sell(60, 15m, new DateTime(2024, 1, 3))
            .Sell(80, 18m, new DateTime(2024, 1, 4))
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        // 第一筆賣出
        Assert.Equal(10m, result.Value[0].BuyPrice);
        Assert.Equal(15m, result.Value[0].SellPrice);
        Assert.Equal(60, result.Value[0].SellQuantity);

        // 第二筆賣出 - 跨批次買入均價
        Assert.Equal(15m, result.Value[1].BuyPrice);
        Assert.Equal(18m, result.Value[1].SellPrice);
        Assert.Equal(80, result.Value[1].SellQuantity);
    }

    [Fact(DisplayName = "已實現損益：賣出數量超過買入數量，回傳失敗")]
    public void CalculateRealizedPnL_SellExceedsBuy_ReturnsFailure()
    {
        // Arrange - 買入 50 股，賣出 100 股，FIFO 迴圈中買入隊列耗盡時觸發衛兵判斷
        var transactions = new TransactionBuilder()
            .Buy(50, 10m, new DateTime(2024, 1, 1))
            .Sell(100, 15m, new DateTime(2024, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultCode.BusinessRuleViolation, result.Code);
    }

    [Fact(DisplayName = "已實現損益：驗證回傳物件的 Id、Date、StockCode、StockMarket 欄位正確對應賣出交易")]
    public void CalculateRealizedPnL_AssertsMappedFields_ReturnsCorrectMetadata()
    {
        // Arrange - 使用美股市場，驗證欄位正確對應
        var sellDate = new DateTime(2024, 3, 15);
        var transactions = new TransactionBuilder("AAPL", StockMarketType.US)
            .Buy(100, 150m, new DateTime(2024, 3, 1))
            .Sell(100, 160m, sellDate)
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);

        var pnl = result.Value![0];
        Assert.Equal(2, pnl.Id);                        // 賣出是第二筆交易，Id = 2
        Assert.Equal(sellDate, pnl.Date);               // 日期來自賣出交易
        Assert.Equal("AAPL", pnl.StockCode);            // 股票代碼來自賣出交易
        Assert.Equal(StockMarketType.US, pnl.StockMarket); // 市場類型來自賣出交易
    }

    [Fact(DisplayName = "已實現損益：買入價高於賣出價，損益為負（亏損）")]
    public void CalculateRealizedPnL_SellPriceLowerThanBuyPrice_ReturnsLoss()
    {
        // Arrange - 買入 100 股 @ $20，賣出 100 股 @ $15，每股亏損 5 元
        var transactions = new TransactionBuilder()
            .Buy(100, 20m, new DateTime(2024, 1, 1))
            .Sell(100, 15m, new DateTime(2024, 1, 2))
            .Build();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);

        var pnl = result.Value![0];
        Assert.Equal(20m, pnl.BuyPrice);
        Assert.Equal(15m, pnl.SellPrice);
        Assert.True(pnl.BuyPrice > pnl.SellPrice, "買入價應大於賣出價，確認為亏損場景");
        Assert.Equal(-500m, (pnl.SellPrice - pnl.BuyPrice) * pnl.SellQuantity); // 總損益 = -500
    }

    [Fact(DisplayName = "已實現損益：空清單，回傳成功且損益清單為空")]
    public void CalculateRealizedPnL_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var result = PositionCalculator.CalculateRealizedPnL(transactions);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    #endregion
}
