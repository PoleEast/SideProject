using Project.Shared.Types;

namespace Project.Data.Model
{
    /// <summary>
    /// 群組動態 - 群組內所有變動的人類可讀敘述紀錄
    /// </summary>
    /// <remarks>
    /// Split Bill 不做權限控制，透明度是它的替代品
    /// （見 ADR 20260731_扁平信任權限與群組動態）。
    /// </remarks>
    public class ActivityLog
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int ActorUserId { get; set; }

        /// <summary>
        /// 變動類型 - 僅供前端決定圖示與顏色
        /// </summary>
        public ActivityActionType ActionType { get; set; }

        public int? TargetExpenseId { get; set; }

        /// <summary>
        /// 含變動前後值的完整中文敘述，由 Service 層組成
        /// </summary>
        /// <remarks>
        /// 例如「將『晚餐』金額由 ¥10,000 改為 ¥8,000，參與者由 3 人改為 4 人」。
        /// 前端直接顯示，不解析字串。存整句而非結構化欄位，是因為動態記錄的是
        /// 當時發生的事實 —— 日後改了幣別格式或欄位名稱，歷史動態也不該跟著變形。
        /// </remarks>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 動態是不可變的事實紀錄，因此刻意不設 UpdatedAt 與 DeletedAt
        /// </summary>
        /// <remarks>
        /// <c>SaveChangesAsync</c> 用反射尋找時間戳欄位，找不到就跳過，因此相容。
        /// 但這也代表 ActivityLog 沒有軟刪除保護 —— 一旦被 Remove() 就是硬刪。
        /// 動態本來就不應被刪除，這是可接受的。
        /// </remarks>
        public DateTime CreatedAt { get; set; }

        public Group Group { get; set; } = null!;
        public User ActorUser { get; set; } = null!;
        public Expense? TargetExpense { get; set; }
    }
}
