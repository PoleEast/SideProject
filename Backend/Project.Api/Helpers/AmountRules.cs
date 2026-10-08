using Project.Shared.Types;

namespace Project.Api.Helpers;

public static class AmountRules
{
    /// <summary>
    /// Split Bill 金額的上限
    /// </summary>
    /// <remarks>
    /// 遠低於金額欄位的容量；超過欄位容量的金額存入時會讓資料庫溢位。
    /// </remarks>
    public const decimal Max = 999999999.99m;

    /// <summary>
    /// 判斷金額是否落在可儲存的範圍內
    /// </summary>
    /// <param name="amount">要檢查的金額</param>
    /// <returns>大於 0 且不超過 <see cref="Max"/> 時為 true</returns>
    public static bool IsInRange(decimal amount) => amount is > 0 and <= Max;

    /// <summary>
    /// 判斷金額是否為幣別最小單位的整數倍
    /// </summary>
    /// <remarks>
    /// 有些幣別沒有小數，帶著零頭的金額付不出去。
    /// </remarks>
    /// <param name="amount">要檢查的金額</param>
    /// <param name="currency">金額的幣別</param>
    /// <returns>小數位數不超過幣別最小單位的小數位數時為 true</returns>
    public static bool FitsMinorUnit(decimal amount, CurrencyType currency) => decimal.Round(amount, currency.MinorUnitDecimals()) == amount;
}
