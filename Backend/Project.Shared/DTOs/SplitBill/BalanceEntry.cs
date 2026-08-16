namespace Project.Shared.DTOs.SplitBill
{
    /// <summary>
    /// 一筆分錄 - 應付方對應收方的應付增加 <c>Amount</c>
    /// </summary>
    /// <param name="DebtorMemberId">應付方</param>
    /// <param name="CreditorMemberId">應收方</param>
    /// <param name="Amount">金額，以基準幣計價，恆為正</param>
    public record BalanceEntry(int DebtorMemberId, int CreditorMemberId, decimal Amount);
}
