using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Expense
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int PayerId { get; set; }
        public CurrencyType CurrencyType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Group Group { get; set; } = null!;
        public GroupMember Payer { get; set; } = null!;

        public ICollection<ExpenseShare> ExpenseShares { get; set; } = [];
    }
}
