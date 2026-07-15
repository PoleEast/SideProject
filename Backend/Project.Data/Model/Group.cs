using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Group
    {
        public int Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsQuickSplit { get; set; }
        public CurrencyType DefaultCurrency { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User OwnerUser { get; set; } = null!;

        public ICollection<GroupMember> GroupMembers { get; set; } = [];
        public ICollection<Expense> Expenses { get; set; } = [];

    }
}
