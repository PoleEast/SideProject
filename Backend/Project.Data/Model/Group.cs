using Project.Shared.Types;

namespace Project.Data.Model
{
    /// <summary>
    /// 群組 - 一組人共同記帳的容器
    /// </summary>
    public class Group
    {
        public int Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 基準幣
        /// </summary>
        public CurrencyType BaseCurrency { get; set; }

        /// <summary>
        /// 邀請碼
        /// </summary>
        public string InviteCode { get; set; } = string.Empty;

        /// <summary>
        /// 已結束時間
        /// </summary>
        public DateTimeOffset? ClosedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<GroupMember> GroupMembers { get; set; } = [];
        public ICollection<Expense> Expenses { get; set; } = [];
        public ICollection<Settlement> Settlements { get; set; } = [];
        public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    }
}
