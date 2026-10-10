using Project.Shared.Types;

namespace Project.Data.Model
{
    /// <summary>
    /// 群組動態 - 群組內所有變動的可讀敘述紀錄
    /// </summary>
    public class ActivityLog
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int ActorUserId { get; set; }

        /// <summary>
        /// 操作者當時所在的成員
        /// </summary>
        public int ActorMemberId { get; set; }

        /// <summary>
        /// 操作者的名稱，寫入當下記下
        /// </summary>
        /// <remarks>
        /// 之後操作者改名、解除綁定或註銷帳號都不改動它。
        /// </remarks>
        public string ActorName { get; set; } = string.Empty;

        public ActivityActionType ActionType { get; set; }
        public int? TargetExpenseId { get; set; }

        /// <summary>
        /// 含變動前後值的完整中文敘述
        /// </summary>
        /// <remarks>
        /// 例如「修改了花費「晚餐」：金額由 JPY 10,000 改為 JPY 8,000、參與者由 3 人改為 4 人」。
        /// </remarks>
        public string Summary { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        public Group Group { get; set; } = null!;

        /// <remarks>
        /// 只用來連結同一次存檔才建立的成員。查詢不可經由它 ——
        /// GroupMember 的 query filter 會讓已移除成員做過的動態從結果中消失。
        /// </remarks>
        public GroupMember ActorMember { get; set; } = null!;

        public Expense? TargetExpense { get; set; }

        public ICollection<ActivityLogDetail> Details { get; set; } = [];
    }
}
