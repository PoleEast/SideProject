using System;
using System.Collections.Generic;
using System.Text;

namespace Project.Data.Model
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int FriendId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Group Group { get; set; } = null!;
        public Friend Friend { get; set; } = null!;
        
        public ICollection<Expense> Expenses { get; set; } = [];
        public ICollection<ExpenseShare> ExpenseShares { get; set; } = [];
    }
}
