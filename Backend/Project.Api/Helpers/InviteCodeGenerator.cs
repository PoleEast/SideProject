using System.Security.Cryptography;

namespace Project.Api.Helpers;

/// <summary>
/// 邀請碼產生器
/// </summary>
/// <remarks>
/// 以 1,000 個群組、每秒 100 次請求估算，6 碼的期望命中時間約 6 小時，8 碼約 327 天。
/// </remarks>
public static class InviteCodeGenerator
{
    public const int CodeLength = 8;

    /// <summary>可用字元</summary>
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// 產生一組邀請碼
    /// </summary>
    /// <returns><see cref="CodeLength"/> 個字元，全部取自 <see cref="Alphabet"/></returns>
    public static string Generate() => RandomNumberGenerator.GetString(Alphabet, CodeLength);
}
