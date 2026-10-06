using System.ComponentModel;

namespace Project.Shared.Types
{
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
