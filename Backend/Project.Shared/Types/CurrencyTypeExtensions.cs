namespace Project.Shared.Types
{
    public static class CurrencyTypeExtensions
    {
        /// <summary>
        /// 幣別最小單位的小數位數
        /// </summary>
        /// <param name="currency">幣別</param>
        /// <returns>台幣與日圓為 0（最小單位 1 元），美元為 2（最小單位 1 分）</returns>
        public static int MinorUnitDecimals(this CurrencyType currency) => currency switch
        {
            CurrencyType.TWD => 0,
            CurrencyType.JPY => 0,
            CurrencyType.USD => 2,
        };
    }
}
