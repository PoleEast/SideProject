namespace Project.Data.Model
{
    /// <summary>
    /// 群組成員 - 群組裡的一個分帳位置，自帶顯示名稱
    /// </summary>
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }

        /// <summary>
        /// 顯示名稱 - 成員清單屬於群組，不依賴任何人的通訊錄
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 綁定的 User - 有值代表能登入共編，null 代表只是一個名字（不需註冊）
        /// </summary>
        /// <remarks>
        /// <c>ExpenseShare</c> 指向 <c>GroupMemberId</c> 而非 <c>UserId</c>，
        /// 因此成員之後註冊並被認領時只需填上此欄位，歷史帳目一筆都不用動。
        /// </remarks>
        public int? UserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Group Group { get; set; } = null!;
        public User? User { get; set; }

        public ICollection<Expense> PaidExpenses { get; set; } = [];
        public ICollection<ExpenseShare> ExpenseShares { get; set; } = [];
    }
}
