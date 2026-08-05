namespace Project.Data.Model
{
    /// <summary>
    /// 分攤 - 某位參與者在某一筆花費中該負擔的金額
    /// </summary>
    public class ExpenseShare
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public int GroupMemberId { get; set; }

        /// <summary>
        /// 分攤額，以原幣計價
        /// </summary>
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Expense Expense { get; set; } = null!;
        public GroupMember GroupMember { get; set; } = null!;
    }
}
