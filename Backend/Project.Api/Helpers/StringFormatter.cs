using Project.Shared.Types;
using System.Globalization;

namespace Project.Api.Helpers;

public static class StringFormatter
{
    /// <summary>
    /// 組成「JPY 10,000」這種幣別代碼加千分位金額的寫法
    /// </summary>
    /// <remarks>
    /// 用代碼而非貨幣符號：台幣與美元會並存於同一個群組，符號分不出是哪一種。
    /// </remarks>
    /// <param name="currency">金額的幣別</param>
    /// <param name="amount">金額</param>
    /// <returns>幣別代碼、空格、千分位金額，小數位數依幣別的最小單位</returns>
    public static string FormatMoney(CurrencyType currency, decimal amount) => $"{currency} {amount.ToString($"N{currency.MinorUnitDecimals()}", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// 組成「「小美」、「阿德」」這種以頓號分隔、各自加上引號的名字清單
    /// </summary>
    /// <param name="names">要列出的名字</param>
    /// <returns>依傳入順序排列的名字清單；沒有名字時為空字串</returns>
    public static string FormatNameList(IEnumerable<string> names) => string.Join("、", names.Select(name => $"「{name}」"));

    /// <summary>
    /// 組成「2026-10-06」這種年月日以連字號分隔的日期寫法
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>yyyy-MM-dd 格式的文字</returns>
    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
