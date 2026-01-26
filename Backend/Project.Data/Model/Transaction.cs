using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User User { get; set; } = null!;
    }
}