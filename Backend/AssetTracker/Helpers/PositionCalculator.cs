using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Position;
using Project.Shared.Types;

namespace AssetTracker.Helpers;

/// <summary>
/// 持倉計算器 - 提供 FIFO 先進先出法的成本與損益計算
/// </summary>
public static class PositionCalculator
{
    /// <summary>
    /// 驗證交易紀錄是否合法（賣出不可大於買入）
    /// </summary>
    /// <param name="transactions">交易清單</param>
    /// <returns>驗證結果</returns>
    public static Result ValidateTransactions(IEnumerable<Transaction> transactions)
    {
        var transactionGroup = transactions.GroupBy(t => new { t.StockMarket, t.StockCode });

        foreach (var group in transactionGroup)
        {
            var quantity = group.Sum(t => t.Type == TransactionType.Buy ? t.Quantity : -t.Quantity);

            if (quantity < 0)
            {
                return Result.Failure(ResultCode.BusinessRuleViolation, "不支援賣出大於買入的情況，請調整交易明細");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 計算持有股數（買入總量 - 賣出總量）
    /// </summary>
    /// <param name="transactions">同一檔股票的交易清單</param>
    /// <returns>淨持有股數</returns>
    public static int CalculateTradeShares(IEnumerable<Transaction> transactions)
    {
        var sellQuantity = transactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.Quantity);
        var buyQuantity = transactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.Quantity);

        return buyQuantity - sellQuantity;
    }

    /// <summary>
    /// 計算平均成本（使用 FIFO 先進先出法）
    /// </summary>
    /// <remarks>
    /// 依買入日期排序，先賣出最早買入的股票，
    /// 剩餘持倉的平均成本 = 剩餘買入金額 / 剩餘數量
    /// </remarks>
    /// <param name="transactions">同一檔股票的交易清單</param>
    /// <returns>平均成本，若無持倉則回傳 0</returns>
    public static decimal CalculateAverageCost(IEnumerable<Transaction> transactions)
    {
        var orderTransactions = transactions.OrderBy(t => t.Date);
        var orderBuyTransactions = orderTransactions.Where(t => t.Type == TransactionType.Buy);
        var sellQuantity = orderTransactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.Quantity);
        var buyQuantity = orderTransactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.Quantity);
        decimal totalAmount = 0;
        var positionQuantity = buyQuantity - sellQuantity;

        if (positionQuantity <= 0) return 0;

        foreach (var buyTransaction in orderBuyTransactions)
        {
            if (buyTransaction.Quantity <= sellQuantity)
            {
                sellQuantity -= buyTransaction.Quantity;
                continue;
            }

            totalAmount += (buyTransaction.Quantity - sellQuantity) * buyTransaction.Price;
            sellQuantity = 0;
        }

        return totalAmount / positionQuantity;
    }

    /// <summary>
    /// 計算單一股票的已實現損益（使用 FIFO 先進先出法）
    /// </summary>
    /// <remarks>
    /// 每筆賣出交易會配對最早的買入交易，
    /// 計算該筆賣出的買入均價 = 消耗的買入總金額 / 賣出數量
    /// </remarks>
    /// <param name="transactions">同一檔股票的交易清單</param>
    /// <returns>每筆賣出交易對應的損益清單</returns>
    public static Result<List<RealizedPnLResponse>> CalculateRealizedPnL(IEnumerable<Transaction> transactions)
    {
        var orderTransactions = transactions.OrderBy(t => t.Date);
        var orderBuyTransactions = new Queue<Transaction>(orderTransactions.Where(t => t.Type == TransactionType.Buy));
        var orderSellTransactions = orderTransactions.Where(t => t.Type == TransactionType.Sell).ToList();

        var result = new List<RealizedPnLResponse>();

        int remainingQuantity = 0;
        decimal remainingPrice = 0;
        foreach (var sellTransaction in orderSellTransactions)
        {
            var realizedPnLResponse = new RealizedPnLResponse
            {
                Id = sellTransaction.Id,
                StockCode = sellTransaction.StockCode,
                StockMarket = sellTransaction.StockMarket,
                Date = sellTransaction.Date,
                SellPrice = sellTransaction.Price,
                SellQuantity = sellTransaction.Quantity,
            };

            decimal buyAmount = 0;
            int sellQuantity = sellTransaction.Quantity;

            while (sellQuantity > 0)
            {
                if (remainingQuantity <= 0)
                {
                    if (orderBuyTransactions.Count == 0) return Result<List<RealizedPnLResponse>>.Failure(ResultCode.BusinessRuleViolation, "賣出數量超過買入數量");

                    var buyTransaction = orderBuyTransactions.Dequeue();
                    remainingQuantity = buyTransaction.Quantity;
                    remainingPrice = buyTransaction.Price;
                }

                if (remainingQuantity > sellQuantity)
                {
                    buyAmount += sellQuantity * remainingPrice;
                    remainingQuantity -= sellQuantity;
                    sellQuantity = 0;
                }
                else
                {
                    buyAmount += remainingQuantity * remainingPrice;
                    sellQuantity -= remainingQuantity;
                    remainingQuantity = 0;
                }
            }

            realizedPnLResponse.BuyPrice = buyAmount / realizedPnLResponse.SellQuantity;
            result.Add(realizedPnLResponse);
        }

        return Result<List<RealizedPnLResponse>>.Success(result);
    }
}
