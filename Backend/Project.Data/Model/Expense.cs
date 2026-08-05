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
        /// 付款人 - 一筆花費只有一位付款人，多人墊款請拆成多筆花費
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

        /// <summary>
        /// 匯率（原幣 → 基準幣）- 建立當下抓取後永不變動
        /// </summary>
        /// <remarks>
        /// 代表消費發生當時的事實，編輯花費時不重抓。原幣與基準幣相同時為 1。
        /// 精度為 (18, 6) 而非其他金額欄位的 (18, 2)：JPY→TWD 約 0.21，
        /// 兩位小數會讓 ¥10,000 的換算誤差達數百元。
        /// </remarks>
        public decimal Rate { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// 建立者 - 僅供顯示「誰新增的」，不作為權限判斷依據
        /// </summary>
        /// <remarks>
        /// Split Bill 採扁平信任，任何成員都可編輯任何花費
        /// （見 ADR 20260731_扁平信任權限與群組動態）。
        /// </remarks>
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
