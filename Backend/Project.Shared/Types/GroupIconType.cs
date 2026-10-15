using System.ComponentModel;

namespace Project.Shared.Types;

public enum GroupIconType
{
    [Description("旅行")]
    Travel,
    [Description("住家")]
    Home,
    [Description("聚餐")]
    Dining,
    [Description("活動")]
    Event,
    [Description("運動")]
    Sports,
    [Description("派對")]
    Party,
    [Description("購物")]
    Shopping,
    [Description("其他")]
    Other
}
