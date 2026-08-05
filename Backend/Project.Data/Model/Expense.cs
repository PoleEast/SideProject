using Project.Shared.Types;

namespace Project.Data.Model
{
    /// <summary>
    /// 花費 - 一筆由某位付款人墊付的支出
    /// </summary>
    public class Expense
    {
        public int Id { get; set; }
        public int GroupId { get; set; }

        /// <summary>
        /// 付款人 - 一筆花費只有一位付款人
        /// </summary>
        public int PayerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExpenseCategoryType Category { get; set; }

        /// <summary>
        /// 原幣 - 這筆花費實際發生時所使用的幣別
        /// </summary>
        public CurrencyType Currency { get; set; }

        /// <summary>
        /// 原幣總額
        /// </summary>
        public decimal Amount { get; set; }

        public decimal Rate { get; set; }
        public DateTime Date { get; set; }
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Group Group { get; set; } = null!;
        public GroupMember Payer { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;

        public ICollection<ExpenseShare> ExpenseShares { get; set; } = [];
    }
}
