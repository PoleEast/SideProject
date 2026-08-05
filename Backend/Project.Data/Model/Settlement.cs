namespace Project.Data.Model
{
    /// <summary>
    /// 還款 - 一位成員實際還錢給另一位的紀錄
    /// </summary>
    /// <remarks>
    /// 記錄的是發生過的事實而非狀態，因此可累積、可撤銷、有時間。
    /// 兩兩淨額 = 花費造成的相欠 − 已還款
    /// （見 ADR 20260731_還款以Settlement記錄表表示）。
    /// </remarks>
    public class Settlement
    {
        public int Id { get; set; }
        public int GroupId { get; set; }

        /// <summary>
        /// 還錢的一方
        /// </summary>
        public int FromMemberId { get; set; }

        /// <summary>
        /// 收錢的一方
        /// </summary>
        public int ToMemberId { get; set; }

        /// <summary>
        /// 還款金額，以群組基準幣計價
        /// </summary>
        /// <remarks>
        /// 基準幣建立後不可變更，因此不需要幣別欄位。
        /// 純量而非布林值，因此部分還款是資料模型的自然結果而非額外功能。
        /// </remarks>
        public decimal Amount { get; set; }

        public DateTime SettledAt { get; set; }

        /// <summary>
        /// 記錄者 - 僅供顯示，不作為權限判斷依據
        /// </summary>
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Group Group { get; set; } = null!;
        public GroupMember FromMember { get; set; } = null!;
        public GroupMember ToMember { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
