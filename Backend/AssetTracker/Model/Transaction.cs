namespace AssetTracker.Model
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}