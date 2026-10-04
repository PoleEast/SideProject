namespace Project.Api.Helpers;

/// <summary>
/// 名字清單在動態敘述與訊息中的寫法
/// </summary>
public static class NameListFormatter
{
    /// <summary>
    /// 組成「「小美」、「阿德」」這種以頓號分隔、各自加上引號的名字清單
    /// </summary>
    public static string Quote(IEnumerable<string> names) => string.Join("、", names.Select(name => $"「{name}」"));
}
