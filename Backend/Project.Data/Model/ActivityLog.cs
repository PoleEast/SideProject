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
        public ActivityActionType ActionType { get; set; }
        public int? TargetExpenseId { get; set; }

        /// <summary>
        /// 含變動前後值的完整中文敘述，由 Service 層組成
        /// </summary>
        /// <remarks>
        /// 例如「將『晚餐』金額由 ¥10,000 改為 ¥8,000，參與者由 3 人改為 4 人」。
        /// </remarks>
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public Group Group { get; set; } = null!;
        public User ActorUser { get; set; } = null!;
        public Expense? TargetExpense { get; set; }
    }
}
