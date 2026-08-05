using System.ComponentModel;

namespace Project.Shared.Types
{
    /// <summary>
    /// 花費分類 - 供群組頁的花費分佈圓餅圖使用
    /// </summary>
    public enum ExpenseCategoryType
    {
        [Description("餐飲")]
        Food,
        [Description("交通")]
        Transport,
        [Description("住宿")]
        Accommodation,
        [Description("購物")]
        Shopping,
        [Description("娛樂")]
        Entertainment,
        [Description("其他")]
        Other
    }
}
