using Project.Shared.Types;

namespace Project.Api.Common
{
    public static class StockConfig
    {

        public record MarketCurrencyMapping(StockMarketType Market, CurrencyType Currency);

        private static readonly MarketCurrencyMapping[] _mappings =
        [
            new(StockMarketType.TW, CurrencyType.TWD),
            new(StockMarketType.US, CurrencyType.USD),
            new(StockMarketType.JP, CurrencyType.JPY),
        ];

        // 靜態建立只執行一次
        private static readonly Dictionary<StockMarketType, CurrencyType> _marketToCurrency =
            _mappings.ToDictionary(m => m.Market, m => m.Currency);

        private static readonly Dictionary<CurrencyType, StockMarketType> _currencyToMarket =
            _mappings.ToDictionary(m => m.Currency, m => m.Market);

        public static CurrencyType? GetCurrency(this StockMarketType market) =>
            _marketToCurrency.TryGetValue(market, out var currency)
                ? currency
                : null;

        public static StockMarketType? GetMarket(this CurrencyType currency) =>
            _currencyToMarket.TryGetValue(currency, out var market)
                ? market
                : null;
    }
}
