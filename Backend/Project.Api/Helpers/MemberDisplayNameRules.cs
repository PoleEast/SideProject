using Project.Shared.Constants;
using Project.Shared.DTOs;
using Project.Shared.Types;

namespace Project.Api.Helpers;

/// <summary>
/// 成員顯示名稱的規則
/// </summary>
public static class MemberDisplayNameRules
{
    private static readonly string InvalidLengthMessage = $"顯示名稱長度請為 1~{MaxLengths.GroupMemberDisplayName} 個字元";

    /// <summary>
    /// 驗證一批候選名稱
    /// </summary>
    /// <param name="candidateNames">使用者輸入的名稱</param>
    /// <param name="takenNames">已被現役成員占用的名稱</param>
    /// <returns>
    /// 成功時為去除前後空白後的名稱；長度不合法回 ValidationError；
    /// 與已占用名稱撞名、或彼此撞名（不分大小寫）回 Conflict，訊息列出撞名的名字
    /// </returns>
    public static Result<List<string>> Validate(IEnumerable<string?> candidateNames, IEnumerable<string> takenNames)
    {
        var displayNames = candidateNames.Select(candidateName => candidateName?.Trim() ?? string.Empty).ToList();

        if (!displayNames.All(displayName => displayName.Length is > 0 and <= MaxLengths.GroupMemberDisplayName))
        {
            return Result<List<string>>.Failure(ResultCode.ValidationError, InvalidLengthMessage);
        }

        var duplicateNames = FindDuplicates(takenNames, displayNames);

        if (duplicateNames.Count > 0)
        {
            return Result<List<string>>.Failure(ResultCode.Conflict, $"以下名稱與其他成員重複：{NameListFormatter.Quote(duplicateNames)}");
        }

        return Result<List<string>>.Success(displayNames);
    }

    /// <summary>
    /// 驗證單一候選名稱，規則同批次版本
    /// </summary>
    public static Result<string> Validate(string? candidateName, IEnumerable<string> takenNames)
    {
        var validation = Validate([candidateName], takenNames);

        return validation.IsSuccess
            ? Result<string>.Success(validation.Value!.Single())
            : Result<string>.Failure(validation);
    }

    /// <summary>
    /// 找出撞名的候選名稱，包含與已占用名稱撞名、以及候選名稱彼此撞名
    /// </summary>
    private static List<string> FindDuplicates(IEnumerable<string> takenNames, IEnumerable<string> candidateNames)
    {
        // Add 失敗等於撞名
        var taken = new HashSet<string>(takenNames, StringComparer.OrdinalIgnoreCase);

        return candidateNames.Where(candidateName => !taken.Add(candidateName)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
