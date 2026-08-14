using Microsoft.EntityFrameworkCore;
using Project.Api.Helpers;
using Project.Data;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Position;

namespace Project.Api.Services;

/// <summary>
/// 持倉與損益查詢服務
/// </summary>
public class PositionService(ApplicationDbContext dbContext)
{
    /// <summary>
    /// 取得使用者的持倉清單
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>持倉清單，僅包含數量大於 0 的股票</returns>
    public async Task<Result<List<PositionResponse>>> GetPositionAsync(int userId)
    {
        var transactions = await dbContext.Transactions.Where(t => t.UserId == userId).ToListAsync();

        if (transactions.Count == 0) return Result<List<PositionResponse>>.Success([]);

        var validateResult = PositionCalculator.ValidateTransactions(transactions);
        if (!validateResult.IsSuccess) return Result<List<PositionResponse>>.Failure(validateResult);

        var transactionGroup = transactions.GroupBy(t => new { t.StockMarket, t.StockCode });
        var positionResponses = transactionGroup.Select(g => new PositionResponse
        {
            StockMarket = g.Key.StockMarket,
            StockCode = g.Key.StockCode,
            AveragePrice = PositionCalculator.CalculateAverageCost(g),
            Quantity = PositionCalculator.CalculateTradeShares(g)
        }).Where(p => p.Quantity > 0).ToList();

        return Result<List<PositionResponse>>.Success(positionResponses);
    }

    /// <summary>
    /// 取得使用者的已實現損益清單
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>每筆賣出交易對應的已實現損益</returns>
    public async Task<Result<List<RealizedPnLResponse>>> GetRealizedPnLAsync(int userId)
    {
        var transactions = await dbContext.Transactions.Where(t => t.UserId == userId).ToListAsync();

        if (transactions.Count == 0) return Result<List<RealizedPnLResponse>>.Success([]);

        var validateResult = PositionCalculator.ValidateTransactions(transactions);
        if (!validateResult.IsSuccess) return Result<List<RealizedPnLResponse>>.Failure(validateResult);

        var transactionGroup = transactions.GroupBy(t => new { t.StockMarket, t.StockCode });
        var result = new List<RealizedPnLResponse>();

        foreach (var group in transactionGroup)
        {
            var realizedPnLResponses = PositionCalculator.CalculateRealizedPnL(group);

            if (!realizedPnLResponses.IsSuccess || realizedPnLResponses.Value == null) return Result<List<RealizedPnLResponse>>.Failure(realizedPnLResponses);

            result.AddRange(realizedPnLResponses.Value);
        }

        return Result<List<RealizedPnLResponse>>.Success(result);
    }
}
