using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public StockMarketType StockMarket { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public User User { get; set; } = null!;
    }
}