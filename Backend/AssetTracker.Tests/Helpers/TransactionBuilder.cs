using Project.Data.Model;
using Project.Shared.Types;

namespace AssetTracker.Tests.Helpers;

/// <summary>
/// 交易資料建構器 - 使用 Fluent API 快速建立測試用交易資料
/// </summary>
public class TransactionBuilder(string stockCode = "2330", StockMarketType market = StockMarketType.TW, int userId = 1)
{
    private readonly List<Transaction> _transactions = [];
    private int _idCounter = 1;
    private readonly int _userId = userId;
    private readonly StockMarketType _market = market;
    private readonly string _stockCode = stockCode;

    /// <summary>
    /// 新增買入交易
    /// </summary>
    /// <param name="quantity">買入數量</param>
    /// <param name="price">買入價格</param>
    /// <param name="date">交易日期（預設為 2024-01-01 起算）</param>
    public TransactionBuilder Buy(int quantity, decimal price, DateTime? date = null)
    {
        _transactions.Add(CreateTransaction(TransactionType.Buy, quantity, price, date));
        return this;
    }

    /// <summary>
    /// 新增賣出交易
    /// </summary>
    /// <param name="quantity">賣出數量</param>
    /// <param name="price">賣出價格</param>
    /// <param name="date">交易日期（預設為 2024-01-01 起算）</param>
    public TransactionBuilder Sell(int quantity, decimal price, DateTime? date = null)
    {
        _transactions.Add(CreateTransaction(TransactionType.Sell, quantity, price, date));
        return this;
    }

    /// <summary>
    /// 建立交易清單
    /// </summary>
    public List<Transaction> Build() => _transactions;

    private Transaction CreateTransaction(TransactionType type, int quantity, decimal price, DateTime? date)
    {
        return new Transaction
        {
            Id = _idCounter++,
            UserId = _userId,
            StockMarket = _market,
            StockCode = _stockCode,
            Type = type,
            Quantity = quantity,
            Price = price,
            Date = date ?? new DateTime(2024, 1, _idCounter),
            CreatedAt = DateTime.UtcNow
        };
    }
}
