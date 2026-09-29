namespace Project.Shared.Types
{
    public static class CurrencyTypeExtensions
    {
        /// <summary>
        /// 幣別最小單位的小數位數 —— 台幣與日圓為 1 元，美元為 1 分
        /// </summary>
        public static int MinorUnitDecimals(this CurrencyType currency) => currency switch
        {
            CurrencyType.TWD => 0,
            CurrencyType.JPY => 0,
            CurrencyType.USD => 2,
        };
    }
}
