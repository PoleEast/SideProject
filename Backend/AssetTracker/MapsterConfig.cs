using Mapster;
using Project.Data.Model;
using Project.Shared.DTOs.Auth;
using Project.Shared.DTOs.ExchangeRate;
using Project.Shared.DTOs.ExchangeRate.ExchangeRateAPI;
using Project.Shared.DTOs.FinMind.StockInfo;
using Project.Shared.DTOs.FinMind.StockPrice;
using Project.Shared.DTOs.Stock;
using Project.Shared.DTOs.Transaction;
using Project.Shared.Types;

namespace AssetTracker
{
    public class MapsterConfig
    {
        public static void SettingGlobalConfig()
        {
            //強制所有目標屬性有來源
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            #region To Entity
            TypeAdapterConfig<RegisterRequest, User>.NewConfig()
                .Map(d => d.Account, s => s.Account)
                .Map(d => d.Name, s => s.Name)
                .Ignore(d => d.Id)
                .Ignore(d => d.PasswordHash)
                .Ignore(d => d.LastLoginAt!)
                .Ignore(d => d.CreatedAt)
                .Ignore(d => d.UpdatedAt!)
                .Ignore(d => d.DeletedAt!)
                .Ignore(d => d.Transactions)
                .Ignore(d => d.Avatars)
                .Ignore(d=>d.Friends)
                .Ignore(d=>d.OwnedGroups);

            TypeAdapterConfig<StockPrice, StockPriceHistory>.NewConfig()
                .Map(d => d.OpeningPrice, s => s.Open)
                .Map(d => d.ClosingPrice, s => s.Close)
                .Map(d => d.High, s => s.Max)
                .Map(d => d.Low, s => s.Min)
                .Map(d => d.Volume, s => s.TradingVolume)
                .Map(d => d.Code, s => s.StockId)
                .Map(d => d.Date, s => s.Date)
                .Ignore(d => d.Id)
                .Ignore(d => d.Exchange)
                .Ignore(d => d.Name)
                .Ignore(d => d.Currency)
                .Ignore(d => d.StockMarket)
                .Ignore(d => d.CreatedAt)
                .Ignore(d => d.UpdatedAt!)
                .Ignore(d => d.DeletedAt!);

            TypeAdapterConfig<PairResponse, ExchangeRateResponse>.NewConfig()
                .Map(d => d.Currency, s => Enum.Parse<CurrencyType>(s.BaseCode))
                .Map(d => d.ConversionRates, s => new Dictionary<CurrencyType, decimal>
                {
                    { Enum.Parse<CurrencyType>(s.TargetCode), s.ConversionRate }
                })
                .Map(d => d.Date, s => DateTimeOffset.FromUnixTimeSeconds(s.TimeLastUpdateUnix).UtcDateTime.Date);

            TypeAdapterConfig<StandardResponse, ExchangeRateResponse>.NewConfig()
                .Map(d => d.Currency, s => Enum.Parse<CurrencyType>(s.BaseCode))
                .Map(d => d.ConversionRates, s => s.ConversionRates
                    .Where(kv => Enum.IsDefined(typeof(CurrencyType), kv.Key))
                    .ToDictionary(kv => Enum.Parse<CurrencyType>(kv.Key), kv => kv.Value))
                .Map(d => d.Date, s => DateTimeOffset.FromUnixTimeSeconds(s.TimeLastUpdateUnix).UtcDateTime.Date);

            #endregion

            #region To DTO

            #region StockInfo
            TypeAdapterConfig<JapanStockInfo, StockInfo>.NewConfig()
                .Map(d => d.Code, s => s.StockId)
                .Map(d => d.Name, s => s.StockName)
                .Map(d => d.IndustryCategory, s => s.Sector)
                .Map(d => d.Exchange, s => s.Exchange);

            TypeAdapterConfig<TaiwanStockInfo, StockInfo>.NewConfig()
                .Map(d => d.Code, s => s.StockId)
                .Map(d => d.Name, s => s.StockName)
                .Map(d => d.IndustryCategory, s => s.IndustryCategory)
                .Map(d => d.Exchange, s => s.Type);

            TypeAdapterConfig<USStockInfo, StockInfo>.NewConfig()
                .Map(d => d.Code, s => s.StockId)
                .Map(d => d.Name, s => s.StockName)
                .Map(d => d.IndustryCategory, s => s.Subsector)
                .Map(d => d.Exchange, _ => "");

            #endregion

            #region StockPrice

            TypeAdapterConfig<TaiwanStockPrice, StockPrice>.NewConfig()
                .Map(d => d.Date, s => s.Date)
                .Map(d => d.StockId, s => s.StockId)
                .Map(d => d.TradingVolume, s => s.TradingVolume)
                .Map(d => d.Open, s => s.Open)
                .Map(d => d.Max, s => s.Max)
                .Map(d => d.Min, s => s.Min)
                .Map(d => d.Close, s => s.Close);

            TypeAdapterConfig<JapanStockPrice, StockPrice>.NewConfig()
                .Map(d => d.Date, s => s.Date)
                .Map(d => d.StockId, s => s.StockId)
                .Map(d => d.TradingVolume, s => s.Volume)
                .Map(d => d.Open, s => s.Open)
                .Map(d => d.Max, s => s.High)
                .Map(d => d.Min, s => s.Low)
                .Map(d => d.Close, s => s.Close);

            TypeAdapterConfig<USStockPrice, StockPrice>.NewConfig()
                .Map(d => d.Date, s => s.Date)
                .Map(d => d.StockId, s => s.StockId)
                .Map(d => d.TradingVolume, s => s.Volume)
                .Map(d => d.Open, s => s.Open)
                .Map(d => d.Max, s => s.High)
                .Map(d => d.Min, s => s.Low)
                .Map(d => d.Close, s => s.Close);

            #endregion

            #endregion

            #region To Response

            TypeAdapterConfig<StockPriceHistory, StockPriceResponse>.NewConfig()
                .Map(d => d.StockMarket, s => s.StockMarket)
                .Map(d => d.Code, s => s.Code)
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.ClosingPrice, s => s.ClosingPrice)
                .Map(d => d.Currency, s => s.Currency)
                .Map(d => d.Date, s => s.Date);

            TypeAdapterConfig<StockInfo, StockInfoResponse>.NewConfig()
                .Map(d => d.Code, s => s.Code)
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.IndustryCategory, s => s.IndustryCategory)
                .Map(d => d.Exchange, s => s.Exchange)
                .Ignore(d => d.StockMarket);

            TypeAdapterConfig<Transaction, TransactionResponse>.NewConfig()
                .Map(d => d.Id, s => s.Id)
                .Map(d => d.StockCode, s => s.StockCode)
                .Map(d => d.Market, s => s.StockMarket)
                .Map(d => d.Date, s => s.Date)
                .Map(d => d.Type, s => s.Type)
                .Map(d => d.Price, s => s.Price)
                .Map(d => d.Quantity, s => s.Quantity)
                .Map(d => d.Remark, s => s.Remark)
                .Map(d => d.CreatedAt, s => s.CreatedAt);

            #endregion

            //啟動時驗證所有配置
            TypeAdapterConfig.GlobalSettings.Compile();
        }
    }
}