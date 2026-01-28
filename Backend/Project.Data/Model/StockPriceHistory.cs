using Project.Shared.Types;

namespace Project.Data.Model
{
    public class StockPriceHistory
    {
        public int Id { get; set; }
        public StockMarketType StockMarket {  get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal OpeningPrice { get; set; }
        public decimal ClosingPrice { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public long Volume { get; set; }
        public CurrencyType Currency { get; set; } 
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

    }
}
