using Project.Shared.Types;

namespace Project.Data.Model
{
    public class ExchangeRateHistory
    {
        public int Id { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal ToUSDRate { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
