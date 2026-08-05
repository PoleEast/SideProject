using Project.Shared.Types;

namespace Project.Data.Model
{
    /// <summary>
    /// 群組 - 一組人共同記帳的容器，Split Bill 唯一一種容器
    /// </summary>
    public class Group
    {
        public int Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 基準幣 - 結算與還款一律以此幣別表示
        /// </summary>
        /// <remarks>
        /// 建立時選定後不可變更，否則所有已鎖入的 <c>Expense.Rate</c> 立刻失去意義
        /// （見 ADR 20260731_SplitBill匯率於建立花費時鎖入）。不可變更由 Service 層把關。
        /// </remarks>
        public CurrencyType BaseCurrency { get; set; }

        /// <summary>
        /// 邀請碼 - 讓其他 User 加入群組並認領既有成員
        /// </summary>
        public string InviteCode { get; set; } = string.Empty;

        /// <summary>
        /// 已結束時間 - 僅影響首頁分區，不限制任何操作
        /// </summary>
        public DateTime? ClosedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User OwnerUser { get; set; } = null!;

        public ICollection<GroupMember> GroupMembers { get; set; } = [];
        public ICollection<Expense> Expenses { get; set; } = [];
        public ICollection<Settlement> Settlements { get; set; } = [];
        public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    }
}
