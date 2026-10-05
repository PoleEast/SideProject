using Project.Shared.Types;
using System.Globalization;

namespace Project.Api.Helpers;

public static class StringFormatter
{
    /// <summary>
    /// 組成「JPY 10,000」這種幣別代碼加千分位金額的寫法，小數位數依幣別的最小單位
    /// </summary>
    /// <remarks>
    /// 金額在動態敘述中的寫法，用代碼而非貨幣符號：台幣與美元會並存於同一個群組，符號分不出是哪一種。
    /// </remarks>
    public static string FormatMoney(CurrencyType currency, decimal amount) => $"{currency} {amount.ToString($"N{currency.MinorUnitDecimals()}", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// 組成「「小美」、「阿德」」這種以頓號分隔、各自加上引號的名字清單
    /// </summary>
    public static string FormatNameList(IEnumerable<string> names) => string.Join("、", names.Select(name => $"「{name}」"));

    /// <summary>
    /// 組成「2026-10-06」這種年月日以連字號分隔的日期寫法
    /// </summary>
    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
