namespace Project.Data.Model
{
    /// <summary>
    /// 還款 - 一位成員實際還錢給另一位的紀錄
    /// </summary>
    /// <remarks>
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
