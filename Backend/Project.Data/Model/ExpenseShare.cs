namespace Project.Data.Model
{
    public class ExpenseShare
    {
        public int Id { get; set; }
        public int GroupMemberId { get; set; }
        public int ExpenseId { get; set; }
        public bool IsPaid { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public GroupMember GroupMember { get; set; } = null!;
        public Expense Expense { get; set; } = null!;
    }
}
