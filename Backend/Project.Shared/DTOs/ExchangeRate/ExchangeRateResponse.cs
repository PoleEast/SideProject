using Project.Shared.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Project.Shared.DTOs.ExchangeRate
{
    public class ExchangeRateResponse
    {
        public CurrencyType Currency { get; set; }
        public Dictionary<CurrencyType, decimal> ConversionRates { get; set; } = [];
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
