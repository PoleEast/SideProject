namespace Project.Shared.DTOs.SplitBill
{
    /// <summary>
    /// 兩位成員之間互相抵銷後的淨額
    /// </summary>
    /// <remarks>
    /// <c>Amount</c> 恆為正，欠款方向由 <c>DebtorMemberId</c> 與 <c>CreditorMemberId</c> 表達。
    /// </remarks>
    /// <param name="DebtorMemberId">欠錢的一方</param>
    /// <param name="CreditorMemberId">被欠的一方</param>
    /// <param name="Amount">尚未清償的金額，以基準幣計價，恆為正</param>
    public record MemberBalance(int DebtorMemberId, int CreditorMemberId, decimal Amount);
}
